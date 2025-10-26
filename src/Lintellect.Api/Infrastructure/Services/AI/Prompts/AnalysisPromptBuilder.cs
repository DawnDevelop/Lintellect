using System.Text;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.Prompts;

/// <summary>
/// Builder for creating structured analysis prompts from analysis results and diffs.
/// Separates prompt construction logic from the analyzer service.
/// </summary>
internal sealed class AnalysisPromptBuilder
{
    private readonly PromptTemplateService _templateService = new();

    /// <summary>
    /// Builds a comprehensive analysis prompt.
    /// </summary>
    public string BuildAnalysisPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
    {
        var sections = new[]
        {
            BuildHeader(),
            BuildStaticAnalysisSection(analysisResult),
            BuildCodeChangesSection(diffs),
            BuildInstructions()
        };

        return _templateService.BuildPrompt(sections);
    }

    /// <summary>
    /// Builds an inline suggestions prompt.
    /// </summary>
    public string BuildInlineSuggestionsPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
    {
        var sections = new[]
        {
            BuildInlineSuggestionsHeader(),
            BuildCodeChangesForReview(diffs),
            BuildStaticAnalyzerFindingsSection(analysisResult),
            BuildInlineSuggestionsInstructions()
        };

        return _templateService.BuildPrompt(sections);
    }

    /// <summary>
    /// Builds a summary prompt.
    /// </summary>
    public string BuildSummaryPrompt(AnalysisRequest analysisResult, Dictionary<string, string> diffs)
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

        if (diffs.Count > 0)
        {
            builder.AppendLine("**Modified Files** (top 5):");
            foreach (var file in diffs.Keys.Take(5))
            {
                builder.AppendLine($"- {file}");
            }
        }

        return builder.ToString();
    }

    private static string BuildHeader()
    {
        return "# Pull Request Analysis Request";
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
        AppendFindingsByCategory(builder, "?? Warnings (Should Fix)", warnings.Take(25).ToList(), includeCodeBlock: false, warnings.Count);

        if (info.Count is > 0 and <= 15)
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
            builder.AppendLine($"- **{finding.RuleId}** at `{finding.FilePath}:{finding.Line}`");

            if (includeCodeBlock)
            {
                builder.AppendLine("  ```");
                builder.AppendLine($"  {finding.Message}");
                builder.AppendLine("  ```");
            }
            else
            {
                builder.AppendLine($"  {finding.Message}");
            }
        }

        if (totalCount.HasValue && totalCount.Value > findings.Count)
        {
            builder.AppendLine($"- ... and {totalCount.Value - findings.Count} more {title.ToLowerInvariant()}");
        }

        builder.AppendLine();
    }

    private static string BuildCodeChangesSection(Dictionary<string, string> diffs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Code Changes");

        if (diffs.Count == 0)
        {
            builder.AppendLine("*No diffs available for review*");
            return builder.ToString();
        }

        builder.AppendLine($"**Files Modified**: {diffs.Count}");
        builder.AppendLine();

        var diffsToShow = diffs.Take(15).ToList();
        foreach (var (filePath, diff) in diffsToShow)
        {
            builder.AppendLine($"### ?? `{filePath}`");
            builder.AppendLine("```diff");

            var diffLines = diff.Split('\n');
            var truncatedDiff = diffLines.Length > 100
                ? string.Join('\n', diffLines.Take(100)) + "\n... (truncated)"
                : diff;

            builder.AppendLine(truncatedDiff);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        if (diffs.Count > 15)
        {
            builder.AppendLine($"... and {diffs.Count - 15} more files changed");
        }

        return builder.ToString();
    }

    private static string BuildInstructions()
    {
        return """
            ---
            **Instructions**: Please provide a comprehensive code review following the structured format. 
            Include code examples for suggested fixes and make your review ready to post as a DevOps PR comment.
            """;
    }

    private static string BuildInlineSuggestionsHeader()
    {
        return """
            # Generate Inline Code Suggestions
            
            ## Your Task:
            Review ALL code changes in the diffs below and generate actionable inline suggestions.
            This includes:
            1. Fixes for the static analyzer findings listed below
            2. **Your own independent code review** - identify issues the static analyzers may have missed:
               - Security vulnerabilities
               - Performance issues
               - Logic errors or bugs
               - Code smells and anti-patterns
               - Missing error handling
               - Potential null reference issues
               - Best practice violations
               - Code quality improvements
            """;
    }

    private static string BuildCodeChangesForReview(Dictionary<string, string> diffs)
    {
        if (diffs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("## Code Changes to Review (Priority: Review Every Line):");
        builder.AppendLine();
        builder.AppendLine("### IMPORTANT: Line Number Format in Diffs");
        builder.AppendLine();
        builder.AppendLine("**Format:** `PREFIX LINENUMBER:CODE_CONTENT`");
        builder.AppendLine();
        builder.AppendLine("**Examples:**");
        builder.AppendLine("- `+42:    var userName = GetUserName();`");
        builder.AppendLine("  - Prefix: `+` (added line)");
        builder.AppendLine("  - Line Number: `42` ? **Use this for your JSON `line` field**");
        builder.AppendLine("  - Code: `    var userName = GetUserName();` ? **Use this for your JSON `suggestedCode` field**");
        builder.AppendLine();
        builder.AppendLine("- `-15:    var oldCode = \"remove\";`");
        builder.AppendLine("  - Prefix: `-` (removed line)");
        builder.AppendLine("  - Line Number: `15`");
        builder.AppendLine("  - Code: `    var oldCode = \"remove\";`");
        builder.AppendLine();
        builder.AppendLine("- ` 20:    var unchanged = \"context\";`");
        builder.AppendLine("  - Prefix: ` ` (space = unchanged context)");
        builder.AppendLine("  - Line Number: `20`");
        builder.AppendLine("  - Code: `    var unchanged = \"context\";`");
        builder.AppendLine();
        builder.AppendLine("**CRITICAL:** When creating suggestions:");
        builder.AppendLine("1. Extract the line number (between prefix and colon) ? put in `line` field");
        builder.AppendLine("2. Extract ONLY the code after the colon ? put in `suggestedCode` field");
        builder.AppendLine("3. NEVER include the line number (e.g., `42:`) in your `suggestedCode`");
        builder.AppendLine();

        foreach (var (filePath, diff) in diffs)
        {
            builder.AppendLine($"### File: `{filePath}`");
            builder.AppendLine("```diff");

            var diffLines = diff.Split('\n');
            var truncatedDiff = diffLines.Length > 100
                ? string.Join('\n', diffLines.Take(100)) + "\n... (truncated)"
                : diff;

            builder.AppendLine(truncatedDiff);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildStaticAnalyzerFindingsSection(AnalysisRequest analysisResult)
    {
        var findingsByFile = analysisResult.Findings
            .Where(f => !string.IsNullOrWhiteSpace(f.FilePath))
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

        foreach (var (filePath, findings) in findingsByFile)
        {
            builder.AppendLine($"### File: `{filePath}`");

            foreach (var finding in findings.OrderBy(f => f.Line))
            {
                builder.AppendLine($"- **Line {finding.Line}** - [{finding.Severity}] {finding.RuleId}");
                builder.AppendLine($"  Message: {finding.Message}");
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string BuildInlineSuggestionsInstructions()
    {
        return """
            ---
            ## Generation Instructions:
            Generate JSON with inline suggestions covering:
            
            **Priority 1: Critical Issues**
            - Security vulnerabilities (SQL injection, XSS, authentication bypass, etc.)
            - Null reference errors
            - Resource leaks (unclosed streams, connections)
            - Logical errors that could cause bugs
            
            **Priority 2: Static Analyzer Findings**
            - Address errors and warnings from the static analysis
            
            **Priority 3: Best Practices & Quality**
            - Performance optimizations
            - Code readability improvements
            - Proper error handling
            - Following language/framework conventions
            
            For each suggestion, include:
            - Exact file path and line number
            - Clear explanation of the issue
            - Corrected code that can be directly applied
            
            Focus on providing value - only suggest changes that meaningfully improve the code.
            """;
    }
}
