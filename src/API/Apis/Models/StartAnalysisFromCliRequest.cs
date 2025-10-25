using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Apis.Models;

/// <summary>
/// Request model for starting analysis from CLI results.
/// </summary>
public sealed record StartAnalysisFromCliRequest(
    AnalysisRequest AnalysisResult);
