using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.cli.Services.Git;
using devops_pr_analyzer.Extensions;
using devops_pr_analyzer.shared.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;

namespace devops_pr_analyzer.cli.Services.Analyzers.Csharp;

internal class CSharpAnalyzer : ICodeAnalyzer
{
    public EProgrammingLanguage Language => EProgrammingLanguage.CSharp;

    public async Task<shared.Models.AnalysisResult> AnalyzeAsync(string solutionPath)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found at path: {solutionPath}");

        // 1. Extract repository archive to temp directory
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(e => Console.WriteLine($"[MSBuild] {e.Diagnostic.Message}"));

        var solution = await workspace.OpenSolutionAsync(solutionPath).ConfigureAwait(false);

        var analyzers = LoadMicrosoftAnalyzers();

        var findings = new List<AnalyzerFindings>();

        var changeDetector = CodeChangeDetectorFactory.Create();
        var changedFiles = changeDetector.GetChangedFiles(["*.cs", "*.json"]);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation == null)
                continue;

            // include compiler diagnostics
            var diagnostics = compilation.GetDiagnostics();

            // include external analyzers
            var withAnalyzers = compilation.WithAnalyzers(analyzers);
            var analyzerDiagnostics =
                await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            diagnostics.AddRange(analyzerDiagnostics);

            foreach (var d in GetFilteredDiagnostics(diagnostics, changedFiles))
            {
                var span = d.Location.GetLineSpan();
                findings.Add(new AnalyzerFindings
                {
                    RuleId = d.Id,
                    Message = d.GetMessage(),
                    FilePath = span.Path,
                    Line = span.StartLinePosition.Line + 1,
                    Severity = d.Severity.ToString()
                });
            }
        }

        return new shared.Models.AnalysisResult
        {
            Language = Language.ToString(),
            Findings = findings
        };
    }

    private static IEnumerable<Diagnostic> GetFilteredDiagnostics(ImmutableArray<Diagnostic> diagnostics, IReadOnlySet<string> changedFiles)
    {
        return diagnostics.Where(d => d.Location.IsInSource &&
                        (d.Severity == DiagnosticSeverity.Warning
                        || d.Severity == DiagnosticSeverity.Error
                        || d.Severity == DiagnosticSeverity.Info) 
                        && !d.Location.SourceTree.FilePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase) &&
                        (changedFiles.Count == 0 || changedFiles.Contains(Path.GetFullPath(d.Location.SourceTree.FilePath))));
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadMicrosoftAnalyzers()
    {
        // Look for NetAnalyzers shipped with your application
        var baseDir = AppContext.BaseDirectory;
        var analyzerDir = Path.Combine(baseDir, "analyzers", "dotnet", "cs");

        if (!Directory.Exists(analyzerDir))
            return [];

        var loader = new AnalyzerAssemblyLoader();
        var dlls = Directory.GetFiles(analyzerDir, "*.dll", SearchOption.AllDirectories);
        var refs = dlls.Select(path => new AnalyzerFileReference(path, loader));
        return [.. refs.SelectMany(r => r.GetAnalyzers(LanguageNames.CSharp))];
    }
}
/// <summary>
/// Minimal loader to replace internal Roslyn FromFileLoader.
/// </summary>
internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly HashSet<string> _deps = [];

    public void AddDependencyLocation(string fullPath) => _deps.Add(fullPath);
    public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
}