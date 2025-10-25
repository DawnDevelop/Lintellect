using devops_pr_analyzer.Infrastructure.Services;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Application.Models;

/// <summary>
/// Complete PR analysis report.
/// </summary>
public sealed class PullRequestAnalysisReportModel
{
    /// <summary>
    /// The original analysis result from static analyzers.
    /// </summary>
    public required AnalysisRequest AnalysisResult { get; init; }

    /// <summary>
    /// Concise summary suitable for PR comments.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Detailed analysis with code suggestions and recommendations.
    /// </summary>
    public required string DetailedAnalysis { get; init; }

    /// <summary>
    /// Statistics about the code changes.
    /// </summary>
    public required DiffStatistics DiffStatistics { get; init; }

    /// <summary>
    /// The AI analyzer that was used.
    /// </summary>
    public required string AnalyzerUsed { get; init; }

    /// <summary>
    /// When the analysis was performed.
    /// </summary>
    public required DateTimeOffset AnalyzedAt { get; init; }

    /// <summary>
    /// Structured inline code suggestions for PR comments.
    /// </summary>
    public string? InlineSuggestions { get; init; }
}

