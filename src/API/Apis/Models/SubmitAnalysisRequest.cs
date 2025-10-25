using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Apis.Models;

/// <summary>
/// Request to submit a new analysis job.
/// </summary>
public sealed record SubmitAnalysisRequest(
    AnalysisRequest CliAnalysisResult);
