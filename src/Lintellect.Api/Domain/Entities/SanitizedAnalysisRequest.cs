using Lintellect.Shared.Models;

namespace Lintellect.Api.Domain.Entities;

/// <summary>
/// Represents a sanitized analysis request with sensitive information removed
/// </summary>
public class SanitizedAnalysisRequest
{
    public EProgrammingLanguage Language { get; set; }
    public IReadOnlyCollection<AnalyzerFindings> Findings { get; set; } = [];
    public GitInfo? GitInfo { get; set; }
    public EGitProvider GitProvider { get; set; }
    public List<string> FileExclusions { get; set; } = [];
    public bool EnableSummaryComment { get; set; } = true;
    public bool EnableInlineSuggestions { get; set; } = true;
    public bool EnableDescriptionSummary { get; set; } = true;
    public bool EnableAzureDevopsCodeOwners { get; set; } = false;

    /// <summary>
    /// Creates a sanitized version of an AnalysisRequest by removing sensitive information
    /// </summary>
    /// <param name="originalRequest">The original analysis request</param>
    /// <returns>A sanitized version without sensitive data</returns>
    public static SanitizedAnalysisRequest FromAnalysisRequest(AnalysisRequest originalRequest)
    {
        return new SanitizedAnalysisRequest
        {
            Language = originalRequest.Language,
            Findings = originalRequest.Findings,
            GitInfo = originalRequest.GitInfo,
            GitProvider = originalRequest.GitProvider,
            FileExclusions = originalRequest.FileExclusions,
            EnableSummaryComment = originalRequest.EnableSummaryComment,
            EnableInlineSuggestions = originalRequest.EnableInlineSuggestions,
            EnableDescriptionSummary = originalRequest.EnableDescriptionSummary,
            EnableAzureDevopsCodeOwners = originalRequest.EnableAzureDevopsCodeOwners
        };
    }
}
