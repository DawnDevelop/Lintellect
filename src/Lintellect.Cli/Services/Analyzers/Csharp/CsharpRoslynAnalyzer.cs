using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using System.Reflection;

namespace Lintellect.Cli.Services.Analyzers.Csharp;

internal class CSharpAnalyzer : ICodeAnalyzer
{
    public EProgrammingLanguage Language => EProgrammingLanguage.CSharp;

    public async Task<Shared.Models.AnalysisRequest> AnalyzeAsync(string solutionPath)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found at path: {solutionPath}");

        // 1. Extract repository archive to temp directory
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(e => Console.WriteLine($"[MSBuild] {e.Diagnostic.Message}"));

        var solution = await workspace.OpenSolutionAsync(solutionPath).ConfigureAwait(false);

        //var analyzers = LoadMicrosoftAnalyzers();

        var findings = new List<AnalyzerFindings>();

        foreach (var project in solution.Projects)
        {
            var analyzers = project.AnalyzerReferences
                .SelectMany(r => r.GetAnalyzers(LanguageNames.CSharp))
                .ToImmutableArray();

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

            foreach (var d in GetFilteredDiagnostics(diagnostics))
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

        return new AnalysisRequest
        {
            Language = Language,
            Findings = findings
        };
    }

    private static IEnumerable<Diagnostic> GetFilteredDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics.Where(d => d.Location.IsInSource &&
                        (d.Severity == DiagnosticSeverity.Warning
                        || d.Severity == DiagnosticSeverity.Error
                        || d.Severity == DiagnosticSeverity.Info) 
                        && !d.Location.SourceTree.FilePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase));
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
