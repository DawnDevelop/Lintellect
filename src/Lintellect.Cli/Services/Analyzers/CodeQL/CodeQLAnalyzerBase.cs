using System.Text.Json;
using Lintellect.Cli.Extensions;
using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;
using Lintellect.Shared.Extensions;

namespace Lintellect.Cli.Services.Analyzers.CodeQL;

internal abstract class CodeQLAnalyzerBase : ICodeAnalyzer
{
    public abstract EProgrammingLanguage Language { get; }

    public async Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath, string? githubToken = null)
    {
        return await AnalyzeAsync(solutionPath, null, githubToken).ConfigureAwait(false);
    }

    public async Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath, List<string>? exclusionPatterns, string? githubToken = null)
    {
        try
        {
            Console.WriteLine($"Initializing CodeQL analysis for {Language}...");

            // Check if CodeQL is installed
            if (!await CodeQLInstaller.EnsureCodeQLInstalledAsync(githubToken).ConfigureAwait(false))
            {
                Console.WriteLine("CodeQL is not installed. Skipping CodeQL analysis.");
                return [];
            }

            // Step 1: Download CodeQL query packs
            await CodeQLExtensions.DownloadCodeQLQueryPacksAsync(Language).ConfigureAwait(false);

            // Step 2: Generate CodeQL database
            var databasePath = await GenerateCodeQLDatabaseAsync(solutionPath).ConfigureAwait(false);

            // Step 3: Run CodeQL queries
            var queryResults = await RunCodeQLQueriesAsync(databasePath).ConfigureAwait(false);

            // Step 4: Parse results and convert to AnalyzerFindings
            var findings = ParseCodeQLResults(queryResults);

            // Step 5: Filter findings based on exclusion patterns
            if (exclusionPatterns != null && exclusionPatterns.Count > 0)
            {
                var filteredFindings = findings.Where(finding =>
                    !FilePatternMatcher.ShouldExclude(finding.FilePath, exclusionPatterns)).ToList();

                var excludedCount = findings.Count - filteredFindings.Count;
                if (excludedCount > 0)
                {
                    Console.WriteLine($"CodeQL analysis: {excludedCount} finding(s) excluded by file patterns");
                }

                findings = filteredFindings;
            }

            Console.WriteLine($"CodeQL analysis completed: {findings.Count} finding(s) detected");

            return findings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CodeQL analysis failed: {ex.Message}");
            Console.WriteLine("Continuing with other analyzers...");
            throw;
        }
    }

    protected abstract Task<string> GenerateCodeQLDatabaseAsync(string solutionPath);

    protected abstract string GetLanguageIdentifier();

    protected abstract string GetSkipDirectories();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task<List<CodeQLResult>> RunCodeQLQueriesAsync(string databasePath)
    {
        Console.WriteLine("Running CodeQL security and quality queries...");

        var results = new List<CodeQLResult>();

        try
        {
            var queryResults = await RunCodeQLQuerySuiteAsync(databasePath).ConfigureAwait(false);
            results.AddRange(queryResults);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to run query suite 'security-extended': {ex.Message}");

        }
        return results;

    }

    protected static void TryValidateDatabasePath(string solutionPath, out string solutionDir, out string databasePath)
    {
        solutionDir = Path.GetDirectoryName(solutionPath) ?? ".";
        databasePath = Path.Combine(solutionDir, "codeql-database");

        // Clean up existing database
        if (Directory.Exists(databasePath))
        {
            Directory.Delete(databasePath, true);
        }
    }
    private async Task<List<CodeQLResult>> RunCodeQLQuerySuiteAsync(string databasePath)
    {
        var resultsPath = Path.GetTempFileName();

        var (success, output, error) = await CodeQLExtensions.ExecuteGitHubCliCommandAsync($"codeql database analyze {databasePath} --format=sarif-latest --output={resultsPath} codeql/{GetLanguageIdentifier()}-queries", "CodeQL Query").ConfigureAwait(false);
        if (!success)
        {
            throw new InvalidOperationException($"CodeQL query execution failed: {error}");
        }

        // Parse SARIF results
        var sarifContent = await File.ReadAllTextAsync(resultsPath).ConfigureAwait(false);
        var sarifResults = JsonSerializer.Deserialize<SarifResult>(sarifContent, JsonOptionExtensions.JsonSerializerOptions);

        return ParseSarifResults(sarifResults);
    }

    private static List<CodeQLResult> ParseSarifResults(SarifResult? sarifResult)
    {
        var results = new List<CodeQLResult>();

        if (sarifResult?.Runs == null)
        {
            return results;
        }

        foreach (var run in sarifResult.Runs)
        {
            // Parse security findings from results
            if (run.Results != null)
            {
                foreach (var result in run.Results)
                {
                    var codeQLResult = new CodeQLResult
                    {
                        RuleId = result.RuleId ?? "unknown",
                        Message = result.Message?.Text ?? "No message",
                        Severity = MapSarifLevelToSeverity(result.Level),
                        FilePath = result.Locations?.FirstOrDefault()?.PhysicalLocation?.ArtifactLocation?.Uri?.Replace("file://", "") ?? "",
                        Line = result.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.StartLine ?? 0,
                        Column = result.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.StartColumn ?? 0,
                        EndLine = result.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.EndLine ?? 0,
                        EndColumn = result.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.EndColumn ?? 0,
                        QueryName = result.RuleId ?? "unknown"
                    };

                    results.Add(codeQLResult);
                }
            }

            // Parse compiler warnings and messages from toolExecutionNotifications
            if (run.Invocations != null)
            {
                foreach (var invocation in run.Invocations)
                {
                    if (invocation.ToolExecutionNotifications != null)
                    {
                        foreach (var notification in invocation.ToolExecutionNotifications)
                        {
                            // Include warnings, errors, and compilation messages that contain "Warning"
                            if (notification.Level == "warning" || notification.Level == "error" ||
                                (notification.Level == "none" && notification.Message?.Text?.Contains("Warning") == true))
                            {
                                var codeQLResult = new CodeQLResult
                                {
                                    RuleId = notification.Descriptor?.Id ?? "compiler-warning",
                                    Message = notification.Message?.Text ?? "No message",
                                    Severity = MapSarifLevelToSeverity(notification.Level),
                                    FilePath = notification.Locations?.FirstOrDefault()?.PhysicalLocation?.ArtifactLocation?.Uri?.Replace("file://", "") ?? "",
                                    Line = notification.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.StartLine ?? 0,
                                    Column = notification.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.StartColumn ?? 0,
                                    EndLine = notification.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.EndLine ?? 0,
                                    EndColumn = notification.Locations?.FirstOrDefault()?.PhysicalLocation?.Region?.EndColumn ?? 0,
                                    QueryName = notification.Descriptor?.Id ?? "compiler-warning"
                                };

                                results.Add(codeQLResult);
                            }
                        }
                    }
                }
            }
        }

        return results;
    }

    private static string MapSarifLevelToSeverity(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "error" => "Error",
            "warning" => "Warning",
            "note" => "Info",
            _ => "Info"
        };
    }

    private static List<AnalyzerFindings> ParseCodeQLResults(List<CodeQLResult> codeQLResults)
    {
        var findings = new List<AnalyzerFindings>();

        foreach (var result in codeQLResults)
        {
            // Skip results without file paths (these are usually project-level issues)
            if (string.IsNullOrEmpty(result.FilePath))
            {
                continue;
            }

            findings.Add(new AnalyzerFindings
            {
                RuleId = $"CodeQL-{result.RuleId}",
                Message = $"[CodeQL] {result.Message}",
                FilePath = result.FilePath,
                Line = result.Line,
                Severity = result.Severity
            });
        }

        return findings;
    }
}

