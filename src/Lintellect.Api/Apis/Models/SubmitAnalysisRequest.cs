using Lintellect.Shared.Models;

namespace Lintellect.Api.Apis.Models;

/// <summary>
/// Request to submit a new analysis job.
/// </summary>
public sealed record SubmitAnalysisRequest(
    AnalysisRequest CliAnalysisResult);
