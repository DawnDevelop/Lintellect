using System.Text;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.Prompts;

/// <summary>
/// Builder for creating structured analysis prompts from analysis results and diffs.
/// Separates prompt construction logic from the analyzer service.
/// </summary>
internal sealed class PromptBuilder
{
    private readonly PromptTemplateService _templateService = new();

    /// <summary>
    /// Builds a comprehensive analysis prompt.
    /// </summary>
    public string BuildAnalysisPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
    {
        var sections = new[]
        {
            BuildCodeChangesForReview(diffs, analysisResult, maxFiles: 15, maxLinesPerFile: 100),
            BuildStaticAnalysisSection(analysisResult)
        };

        return _templateService.BuildPrompt(sections);
    }

    /// <summary>
    /// Builds an inline suggestions prompt.
    /// </summary>
    public string BuildInlineSuggestionsPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
    {
        // Get prioritized files to ensure findings match the files shown in diffs
        var prioritizedFiles = PrioritizeFiles(diffs, [.. analysisResult.Findings], maxFiles: 15);
        var includedFilePaths = prioritizedFiles.Select(kvp => kvp.Key).ToHashSet();

        var sections = new[]
        {
            BuildCodeChangesForReview(diffs, analysisResult, maxFiles: 15, maxLinesPerFile: 1000),
            BuildStaticAnalyzerFindingsSection(analysisResult, includedFilePaths),
        };

        return _templateService.BuildPrompt(sections);
    }

    /// <summary>
    /// Builds a summary prompt.
    /// </summary>
    public static string BuildSummaryPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Generate a concise PR summary for the following:");
        builder.AppendLine();

        if (analysisResult.GitInfo is not null)
        {
            builder.AppendLine($"**PR**: #{analysisResult.GitInfo.PullRequestId} in {analysisResult.GitInfo.RepositoryName}");
        }

        var errors = analysisResult.Findings.Count(f => f.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        var warnings = analysisResult.Findings.Count(f => f.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));

        builder.AppendLine($"**Language**: {analysisResult.Language}");
        builder.AppendLine($"**Findings**: {errors} errors, {warnings} warnings");
        builder.AppendLine($"**Files Changed**: {diffs.Count}");
        builder.AppendLine();

        if (errors > 0)
        {
            builder.AppendLine("**Critical Issues**:");
            foreach (var finding in analysisResult.Findings
                .Where(f => f.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase))
                .Take(3))
            {
                builder.AppendLine($"- {finding.RuleId}: {finding.Message.Split('\n')[0]}");
            }
            builder.AppendLine();
        }

        builder.AppendLine(BuildCodeChangesForReview(diffs, analysisResult, maxFiles: 15, maxLinesPerFile: 100));

        return builder.ToString();
    }


    private static string BuildStaticAnalysisSection(AnalysisRequest analysisResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Static Analysis Results");
        builder.AppendLine($"- **Language**: {analysisResult.Language}");

        var errors = analysisResult.Findings.Where(f => f.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase)).ToList();
        var warnings = analysisResult.Findings.Where(f => f.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase)).ToList();
        var info = analysisResult.Findings.Where(f => f.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase)).ToList();

        builder.AppendLine($"- **Total Findings**: {analysisResult.Findings.Count}");
        builder.AppendLine($"  - ?? Errors: {errors.Count}");
        builder.AppendLine($"  - ?? Warnings: {warnings.Count}");
        builder.AppendLine($"  - ?? Info: {info.Count}");
        builder.AppendLine();

        AppendFindingsByCategory(builder, "?? Errors (Must Fix)", errors, includeCodeBlock: true);
        AppendFindingsByCategory(builder, "?? Warnings (Should Fix)", [.. warnings.Take(25)], includeCodeBlock: false, warnings.Count);

        if (info.Count is > 0 and <= 10)
        {
            AppendFindingsByCategory(builder, "?? Informational Messages", info, includeCodeBlock: false);
        }

        return builder.ToString();
    }

    private static void AppendFindingsByCategory(
        StringBuilder builder,
        string title,
        List<AnalyzerFindings> findings,
        bool includeCodeBlock,
        int? totalCount = null)
    {
        if (findings.Count == 0)
        {
            return;
        }

        builder.AppendLine($"### {title}");

        foreach (var finding in findings)
        {
            // Compact one-line form: "- {file}:{line} [{sev}] {ruleId}: {msg}" — half the tokens
            // of the previous two-line layout while preserving everything the model needs.
            var sev = ShortSeverity(finding.Severity);
            var message = SummarizeMessage(finding.Message);
            builder.AppendLine($"- {finding.FilePath}:{finding.Line} [{sev}] {finding.RuleId}: {message}");
        }

        if (totalCount.HasValue && totalCount.Value > findings.Count)
        {
            builder.AppendLine($"- ... and {totalCount.Value - findings.Count} more {title.ToLowerInvariant()}");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// Prioritizes files based on findings severity: errors > warnings > info > none.
    /// Returns top N files with findings prioritized first.
    /// </summary>
    private static List<KeyValuePair<string, string>> PrioritizeFiles(
        Dictionary<string, string> diffs,
        List<AnalyzerFindings> findings,
        int maxFiles)
    {
        if (findings.Count == 0 || diffs.Count <= maxFiles)
        {
            return [.. diffs.Take(maxFiles)];
        }

        // Group findings by file and calculate priority score
        var fileScores = new Dictionary<string, int>();
        foreach (var finding in findings.Where(f => diffs.ContainsKey(f.FilePath)))
        {
            if (!fileScores.ContainsKey(finding.FilePath))
            {
                fileScores[finding.FilePath] = 0;
            }

            // Score: errors=10, warnings=5, info=1
            fileScores[finding.FilePath] += finding.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase) ? 10
                : finding.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase) ? 5
                : finding.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase) ? 1
                : 0;
        }

        // Sort files: high score first, then by name for consistency
        var prioritized = diffs
            .OrderByDescending(kvp => fileScores.GetValueOrDefault(kvp.Key, 0))
            .ThenBy(kvp => kvp.Key)
            .Take(maxFiles)
            .ToList();

        return prioritized;
    }


    private static string BuildCodeChangesForReview(
        Dictionary<string, string> diffs,
        AnalysisRequest? analysisResult,
        int maxFiles,
        int maxLinesPerFile)
    {
        if (diffs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("## Code Changes to Review (Priority: Review Every Line):");
        builder.AppendLine();
        builder.AppendLine("**Note:** Each diff line is prefixed with `<new-file line number>|<diff marker><code>`. Use the number before the `|` directly for `lineFrom`/`lineTo` — do not calculate it. Removed lines have a blank number. See system prompt for details.");
        builder.AppendLine();

        // Prioritize files based on findings: errors > warnings > info > none
        var prioritizedFiles = PrioritizeFiles(diffs, analysisResult?.Findings.ToList() ?? [], maxFiles);

        foreach (var (filePath, diff) in prioritizedFiles)
        {
            builder.AppendLine($"### File: `{filePath}`");
            builder.AppendLine("```diff");

            var annotatedDiff = DiffGenerationHelper.AnnotateWithLineNumbers(diff);
            var diffLines = annotatedDiff.Split('\n');
            var truncatedDiff = diffLines.Length > maxLinesPerFile
                ? string.Join('\n', diffLines.Take(maxLinesPerFile)) + "\n... (truncated)"
                : annotatedDiff;

            builder.AppendLine(truncatedDiff);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        if (diffs.Count > prioritizedFiles.Count)
        {
            builder.AppendLine($"... and {diffs.Count - prioritizedFiles.Count} more files changed");
        }

        return builder.ToString();
    }

    private static string BuildStaticAnalyzerFindingsSection(
        AnalysisRequest analysisResult,
        HashSet<string>? includedFilePaths)
    {
        var findings = analysisResult.Findings
            .Where(f => !string.IsNullOrWhiteSpace(f.FilePath));

        // Filter to only include files present in the truncated diff section
        if (includedFilePaths != null && includedFilePaths.Count > 0)
        {
            findings = findings.Where(f => includedFilePaths.Contains(f.FilePath));
        }

        var findingsByFile = findings
            .GroupBy(f => f.FilePath)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (findingsByFile.Count == 0)
        {
            return """
                ## Static Analyzer Findings:
                *No static analyzer findings - focus on comprehensive code review of all changes*
                """;
        }

        var builder = new StringBuilder();
        builder.AppendLine("## Static Analyzer Findings (Reference Only - Also Review Beyond These):");
        builder.AppendLine();

        foreach (var (filePath, fileFindings) in findingsByFile.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"### {filePath}");

            foreach (var finding in fileFindings.OrderBy(f => f.Line))
            {
                // Compact one-line form: "- L42 [W] CA1234: msg"
                var sev = ShortSeverity(finding.Severity);
                builder.AppendLine($"- L{finding.Line} [{sev}] {finding.RuleId}: {SummarizeMessage(finding.Message)}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ShortSeverity(string? severity) => severity?.ToUpperInvariant() switch
    {
        "ERROR" => "E",
        "WARNING" => "W",
        "INFO" => "I",
        _ => "?"
    };

    private static string SummarizeMessage(string? message, int maxLength = 150)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var trimmed = message.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        var firstSentence = trimmed
            .Split(['.', '!', '?'], 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();

        if (!string.IsNullOrEmpty(firstSentence) && firstSentence.Length <= maxLength / 2)
        {
            return firstSentence.EndsWith('.') ? firstSentence : firstSentence + ".";
        }

        return trimmed[..maxLength].TrimEnd() + "...";
    }

}
