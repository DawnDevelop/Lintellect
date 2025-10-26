using Lintellect.Shared.Models;

namespace Lintellect.Api.Apis.Models;

/// <summary>
/// Request model for starting analysis from CLI results.
/// </summary>
public sealed record StartAnalysisFromCliRequest(
    AnalysisRequest AnalysisResult);
