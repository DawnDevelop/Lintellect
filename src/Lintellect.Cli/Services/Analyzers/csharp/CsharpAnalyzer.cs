using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace Lintellect.Cli.Services.Analyzers.Csharp;

internal sealed class CSharpAnalyzer : ICodeAnalyzer
{
    public EProgrammingLanguage Language => EProgrammingLanguage.CSharp;

    public async Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath)
    {
        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException($"Solution file not found at path: {solutionPath}");
        }

        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        using var workspace = MSBuildWorkspace.Create();

        workspace.RegisterWorkspaceFailedHandler(
            x => System.Diagnostics.Debug.WriteLine($"[MSBuild] {x.Diagnostic.Message}"
        ));


        var solution = await workspace.OpenSolutionAsync(solutionPath).ConfigureAwait(false);

        // Load analyzers ONCE (external + project-provided)
        var externalAnalyzers = LoadExternalAnalyzers();
        var projectAnalyzers = solution.Projects
            .SelectMany(p => p.AnalyzerReferences)
            .Distinct()
            .SelectMany(r => r.GetAnalyzers(LanguageNames.CSharp));

        var analyzers = externalAnalyzers.AddRange(projectAnalyzers).Distinct(AnalyzerIdentityComparer.Instance).ToImmutableArray();

        // Analyzer options & execution policy
        var options = new CompilationWithAnalyzersOptions(
            options: solution.Projects.FirstOrDefault()?.AnalyzerOptions ?? new AnalyzerOptions([]),
            onAnalyzerException: null,
            analyzerExceptionFilter: null,
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false // honor suppressions
        );

        var findings = new ConcurrentBag<AnalyzerFindings>();

        // Bounded parallelism (safe over compilations; workspace reads only)
        var degree = Math.Max(1, Environment.ProcessorCount - 1);
        await Parallel.ForEachAsync(solution.Projects, new ParallelOptions { MaxDegreeOfParallelism = degree },
            async (project, token) =>
            {
                var compilation = await project.GetCompilationAsync(token).ConfigureAwait(false);
                if (compilation is null)
                {
                    return;
                }

                var cwa = compilation.WithAnalyzers(analyzers, options);
                // Includes compiler + analyzer diagnostics; no need to call GetDiagnostics() separately.
                var allDiagnostics = await cwa.GetAllDiagnosticsAsync(token).ConfigureAwait(false);

                foreach (var d in GetFilteredDiagnostics(allDiagnostics))
                {
                    var span = d.Location.GetMappedLineSpan(); // respects #line and generated mappings
                    findings.Add(new AnalyzerFindings
                    {
                        RuleId = d.Id,
                        Message = d.GetMessage(CultureInfo.InvariantCulture),
                        FilePath = NormalizePath(span.Path),
                        Line = span.StartLinePosition.Line + 1,
                        Severity = d.Severity.ToString()
                    });
                }
            }).ConfigureAwait(false);

        // de-dupe same diag reported twice (rare but can happen with linked files)
        var distinct = findings
            .GroupBy(f => (f.RuleId, f.FilePath, f.Line, f.Message, f.Severity), StringTupleComparer.Instance)
            .Select(g => g.First())
            .ToList();

        return distinct;
    }

    private static IEnumerable<Diagnostic> GetFilteredDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
        {
            if (!d.Location.IsInSource)
            {
                return false;
            }

            var path = d.Location.SourceTree?.FilePath;
            return !string.IsNullOrEmpty(path) && (d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error or DiagnosticSeverity.Info)
                   && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadExternalAnalyzers()
    {
        var baseDir = AppContext.BaseDirectory;
        var analyzerDir = Path.Combine(baseDir, "analyzers", "dotnet", "cs");
        if (!Directory.Exists(analyzerDir))
        {
            return ImmutableArray<DiagnosticAnalyzer>.Empty;
        }

        var loader = new AnalyzerAssemblyLoader();
        var refs = Directory.EnumerateFiles(analyzerDir, "*.dll", SearchOption.AllDirectories)
                            .Select(path => new AnalyzerFileReference(path, loader));
        return refs.SelectMany(r => r.GetAnalyzers(LanguageNames.CSharp)).ToImmutableArray();
    }

    // Distinct by analyzer type/identity
    private sealed class AnalyzerIdentityComparer : IEqualityComparer<DiagnosticAnalyzer>
    {
        public static AnalyzerIdentityComparer Instance { get; } = new();
        public bool Equals(DiagnosticAnalyzer? x, DiagnosticAnalyzer? y)
        {
            return x?.GetType().FullName == y?.GetType().FullName && x?.GetType().Assembly.Location == y?.GetType().Assembly.Location;
        }

        public int GetHashCode(DiagnosticAnalyzer obj)
        {
            return HashCode.Combine(obj.GetType().FullName, obj.GetType().Assembly.Location);
        }
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string RuleId, string FilePath, int Line, string Message, string Severity)>
    {
        public static StringTupleComparer Instance { get; } = new();
        public bool Equals((string, string, int, string, string) x, (string, string, int, string, string) y)
        {
            return string.Equals(x.Item1, y.Item1, StringComparison.Ordinal)
                    && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase)
                    && x.Item3 == y.Item3
                    && string.Equals(x.Item4, y.Item4, StringComparison.Ordinal)
                    && string.Equals(x.Item5, y.Item5, StringComparison.Ordinal);
        }

        public int GetHashCode((string, string, int, string, string) t)
        {
            return HashCode.Combine(t.Item1, t.Item2.ToLowerInvariant(), t.Item3, t.Item4, t.Item5);
        }
    }
}

/// <summary>Minimal loader for AnalyzerFileReference.</summary>
internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly HashSet<string> _deps = new(StringComparer.OrdinalIgnoreCase);
    public void AddDependencyLocation(string fullPath)
    {
        _deps.Add(fullPath);
    }

    public Assembly LoadFromPath(string fullPath)
    {
        return Assembly.LoadFrom(fullPath);
    }
}
