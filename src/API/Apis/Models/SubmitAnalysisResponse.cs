namespace devops_pr_analyzer.Apis.Models;

/// <summary>
/// Response when submitting an analysis job.
/// </summary>
public sealed record SubmitAnalysisResponse(
    Guid JobId, 
    string Status, 
    string Message);
