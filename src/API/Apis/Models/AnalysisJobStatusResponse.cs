using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Apis.Models;

public sealed record AnalysisJobStatusResponse(
    Guid JobId,
    string Status,
    string ProjectName,
    string RepositoryName,
    int PullRequestId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null,
    AnalysisRequest? AnalysisResult = null,
    string? Summary = null,
    string? DetailedAnalysis = null,
    string? InlineSuggestions = null,
    string? AnalyzerUsed = null
);