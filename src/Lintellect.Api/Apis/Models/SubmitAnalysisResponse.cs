namespace Lintellect.Api.Apis.Models;

/// <summary>
/// Response when submitting an analysis job.
/// </summary>
public sealed record SubmitAnalysisResponse(
    Guid JobId, 
    string Status, 
    string Message);
