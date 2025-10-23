using devops_pr_analyzer.Interfaces;
using devops_pr_analyzer.Models;
using devops_pr_analyzer.Services.Git;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Services;

/// <summary>
/// Orchestrates the complete PR analysis workflow by coordinating Git diff retrieval and AI analysis.
/// </summary>
public sealed class PullRequestAnalysisOrchestrator(
    PullRequestService prService,
    IAnalyzerServiceResolver analyzerResolver,
    ILogger<PullRequestAnalysisOrchestrator> logger)
{
    private readonly PullRequestService _diffService = prService ?? throw new ArgumentNullException(nameof(prService));
    private readonly IAnalyzerServiceResolver _analyzerResolver = analyzerResolver ?? throw new ArgumentNullException(nameof(analyzerResolver));
    private readonly ILogger<PullRequestAnalysisOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Performs a complete PR analysis including diff retrieval and AI review.
    /// </summary>
    /// <param name="analysisResult">The analysis result from static code analyzers</param>
    /// <param name="analyzerType">The AI analyzer to use (Claude, AIFoundry, etc.)</param>
    /// <param name="options">Optional configuration for diff retrieval and analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete analysis report including summary and detailed review</returns>
    public async Task<PullRequestAnalysisReportModel> AnalyzeAsync(
        AnalysisResult analysisResult,
        EAnalyzers analyzerType,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= AnalysisOptions.Default;
        
        _logger.LogInformation(
            "Starting PR analysis for {Repository} PR #{PullRequest} using {Analyzer}",
            analysisResult.GitInfo?.RepositoryName ?? "Unknown",
            analysisResult.GitInfo?.Identifier ?? "Unknown",
            analyzerType);

        try
        {
            // Step 1: Retrieve diffs from Git provider
            _logger.LogDebug("Retrieving diffs from Git provider...");
            var diffs = await GetDiffsAsync(analysisResult, options, cancellationToken);

            _logger.LogInformation("Retrieved {DiffCount} file diffs", diffs.Count);

            // Step 2: Filter findings to only include files in the PR
            var filteredAnalysisResult = FilterFindingsByPullRequestFiles(analysisResult, diffs);
            
            if (filteredAnalysisResult.Findings.Count != analysisResult.Findings.Count)
            {
                _logger.LogInformation(
                    "Filtered findings: {OriginalCount} -> {FilteredCount} (removed {RemovedCount} findings for files not in PR)",
                    analysisResult.Findings.Count,
                    filteredAnalysisResult.Findings.Count,
                    analysisResult.Findings.Count - filteredAnalysisResult.Findings.Count);
            }

            // Step 3: Get the appropriate AI analyzer
            var analyzer = _analyzerResolver.GetAnalyzerService(analyzerType);

            // Step 4: Generate summary (quick, for PR overview)
            var customInstructions = await prService.GetCustomInstructionsAsync(filteredAnalysisResult);

            var aiAnalyzerModel = new AnalyzerServiceModel(filteredAnalysisResult, customInstructions ?? string.Empty);

            var summary = string.Empty;
            if (options.IncludeSummary)
            {
                summary = await CreateAndAddSummaryAsync(filteredAnalysisResult, diffs, analyzer, aiAnalyzerModel, cancellationToken);
            }

            // Step 5: Generate detailed analysis (comprehensive review)

            var detailedAnalysis = string.Empty;
            if(options.IncludeComprehensiveComment)
            {
                detailedAnalysis = await CreateAndAddComprehensiveCommentAsync(filteredAnalysisResult, diffs, analyzer, aiAnalyzerModel, cancellationToken);
            }

            if(options.IncludeInlineSuggestions)
            {
                var suggestions = await analyzer.GenerateInlineSuggestionsAsync(aiAnalyzerModel, diffs, cancellationToken);
                foreach (var suggestion in suggestions.Where(x => !string.IsNullOrWhiteSpace(x.SuggestedCode)))
                {
                    // Build context from title, rule ID, and explanation
                    var context = BuildSuggestionContext(suggestion);
                    
                    await prService.AddInlineSuggestionAsync(
                        filteredAnalysisResult, 
                        suggestion.SuggestedCode, 
                        context,
                        suggestion.FilePath, 
                        suggestion.LineFrom,
                        suggestion.LineTo)  // Support multi-line suggestions
                        ;
                }
            }


            return new PullRequestAnalysisReportModel
            {
                AnalysisResult = filteredAnalysisResult,
                Summary = summary,
                DetailedAnalysis = detailedAnalysis,
                DiffStatistics = BuildDiffStatistics(diffs),
                AnalyzerUsed = analyzerType.ToString(),
                AnalyzedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze PR for {Repository}", 
                analysisResult.GitInfo?.RepositoryName ?? "Unknown");
            throw;
        }
    }

    /// <summary>
    /// Filters the analysis result to only include findings for files that are part of the pull request.
    /// </summary>
    /// <param name="analysisResult">The original analysis result with all findings</param>
    /// <param name="prFiles">Dictionary of files in the pull request (file paths as keys)</param>
    /// <returns>A new AnalysisResult containing only findings for files in the PR</returns>
    private AnalysisResult FilterFindingsByPullRequestFiles(
        AnalysisResult analysisResult,
        Dictionary<string, string> prFiles)
    {
        if (prFiles.Count == 0)
        {
            _logger.LogWarning("No files in pull request diff, returning empty findings");
            return new AnalysisResult
            {
                Language = analysisResult.Language,
                Findings = [],
                GitInfo = analysisResult.GitInfo,
                GitProvider = analysisResult.GitProvider
            };
        }

        // Normalize PR file paths for comparison (handle different path separators and casing)
        var normalizedPrFilePaths = new HashSet<string>(
            prFiles.Keys.Select(NormalizeFilePath),
            StringComparer.OrdinalIgnoreCase);

        // Filter findings to only those whose file paths exist in the PR
        var filteredFindings = analysisResult.Findings
            .Where(finding => 
            {
                if (string.IsNullOrWhiteSpace(finding.FilePath))
                    return false;

                var normalizedFindingPath = NormalizeFilePath(finding.FilePath);
                
                // First try exact match
                if (normalizedPrFilePaths.Contains(normalizedFindingPath))
                    return true;

                // If the finding has an absolute path, try matching just the filename
                // or the relative portion at the end of the path
                return normalizedPrFilePaths.Any(prPath => 
                    normalizedFindingPath.EndsWith(prPath, StringComparison.OrdinalIgnoreCase) ||
                    prPath.EndsWith(normalizedFindingPath, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        return new AnalysisResult
        {
            Language = analysisResult.Language,
            Findings = filteredFindings,
            GitInfo = analysisResult.GitInfo,
            GitProvider = analysisResult.GitProvider
        };
    }

    /// <summary>
    /// Normalizes a file path for consistent comparison across platforms.
    /// Converts backslashes to forward slashes and removes leading slashes.
    /// </summary>
    private static string NormalizeFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        // Convert backslashes to forward slashes
        var normalized = filePath.Replace('\\', '/');
        
        // Remove leading slash if present
        if (normalized.StartsWith('/'))
            normalized = normalized[1..];

        return normalized;
    }

    private async Task<string> CreateAndAddComprehensiveCommentAsync(AnalysisResult analysisResult, Dictionary<string, string> diffs, IAnalyzerService analyzer, AnalyzerServiceModel aiAnalyzerModel, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating detailed analysis...");
        var detailedAnalysis = await analyzer.AnalyzeAsync(
            aiAnalyzerModel,
            diffs,
            cancellationToken);
        await prService.AddCommentAsync(analysisResult, detailedAnalysis);
        _logger.LogInformation("Detailed analysis generated ({Length} chars)", detailedAnalysis.Length);

        return detailedAnalysis;
    }

    private async Task<string> CreateAndAddSummaryAsync(AnalysisResult analysisResult, Dictionary<string, string> diffs, IAnalyzerService analyzer, AnalyzerServiceModel aiAnalyzerModel, CancellationToken cancellationToken)
    {
        string summary = string.Empty;

        _logger.LogDebug("Generating PR summary...");
        summary = await analyzer.GenerateSummaryAsync(
            aiAnalyzerModel,
            diffs,
            cancellationToken);
        _logger.LogInformation("PR summary generated ({Length} chars)", summary.Length);

        if (!string.IsNullOrWhiteSpace(summary))
        {
            await prService.AppendToDescriptionAsync(analysisResult, summary);
        }
        else
        {
            _logger.LogWarning("Generated summary is empty");
        }
        

        return summary;
    }

    /// <summary>
    /// Generates only a quick summary without detailed analysis.
    /// Useful for fast feedback in PR comments.
    /// </summary>
    public async Task<string> GenerateQuickSummaryAsync(
        AnalysisResult analysisResult,
        EAnalyzers analyzerType,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= AnalysisOptions.Default;

        _logger.LogInformation("Generating quick summary for PR #{PullRequest}",
            analysisResult.GitInfo?.Identifier ?? "Unknown");

        var diffs = await GetDiffsAsync(analysisResult, options, cancellationToken);
        
        // Filter findings to only include files in the PR
        var filteredAnalysisResult = FilterFindingsByPullRequestFiles(analysisResult, diffs);
        
        var analyzer = _analyzerResolver.GetAnalyzerService(analyzerType);

        return await analyzer.GenerateSummaryAsync(new AnalyzerServiceModel(filteredAnalysisResult, string.Empty), diffs, cancellationToken)
            ;
    }


    private async Task<Dictionary<string, string>> GetDiffsAsync(
        AnalysisResult analysisResult,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _diffService.GetCompactDiffsAsync(
                analysisResult,
                contextLines: options.ContextLines,
                maxNewFileLines: options.MaxNewFileLines,
                maxLinesPerFile: options.MaxLinesPerFile)
                ;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve diffs, continuing with empty diffs");
            return [];
        }
    }

    private static DiffStatistics BuildDiffStatistics(Dictionary<string, string> diffs)
    {
        int filesChanged = diffs.Count;
        int linesAdded = 0;
        int linesRemoved = 0;

        foreach (var diff in diffs.Values)
        {
            var lines = diff.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                    linesAdded++;
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                    linesRemoved++;
            }
        }

        return new DiffStatistics
        {
            FilesChanged = filesChanged,
            LinesAdded = linesAdded,
            LinesRemoved = linesRemoved
        };
    }

    /// <summary>
    /// Builds formatted context text for inline suggestions.
    /// </summary>
    private static string BuildSuggestionContext(InlineSuggestion suggestion)
    {
        var severityEmoji = suggestion.Severity?.ToLowerInvariant() switch
        {
            "error" => "🔴",
            "warning" => "🟡",
            "info" => "🔵",
            _ => "💡"
        };

        var context = $"{severityEmoji} **{suggestion.Title}**";
        
        if (!string.IsNullOrWhiteSpace(suggestion.RuleId))
        {
            context += $"\nRule: `{suggestion.RuleId}`";
        }

        context += $"\n\n{suggestion.Explanation}";
        
        return context;
    }
}

/// <summary>
/// Configuration options for PR analysis.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>
    /// Number of context lines around changes in diffs.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Maximum lines to show for new/deleted files.
    /// </summary>
    public int MaxNewFileLines { get; init; } = 50;

    /// <summary>
    /// Maximum total lines per file diff.
    /// </summary>
    public int MaxLinesPerFile { get; init; } = 1000;

    public bool IncludeSummary { get; init; } = true;

    public bool IncludeComprehensiveComment { get; init; } = true;

    public bool IncludeInlineSuggestions { get; init; } = true;
    /// <summary>
    /// Default analysis options.
    /// </summary>
    public static AnalysisOptions Default => new();

    /// <summary>
    /// Comprehensive options for detailed analysis.
    /// </summary>
    public static AnalysisOptions Comprehensive => new()
    {
        ContextLines = 80,
        MaxNewFileLines = 1000,
        MaxLinesPerFile = 2000
    };
}


/// <summary>
/// Statistics about code changes in the PR.
/// </summary>
public sealed class DiffStatistics
{
    /// <summary>
    /// Number of files changed.
    /// </summary>
    public required int FilesChanged { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public required int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines removed.
    /// </summary>
    public required int LinesRemoved { get; init; }

    /// <summary>
    /// Total lines changed (added + removed).
    /// </summary>
    public int TotalChanges => LinesAdded + LinesRemoved;
}
