namespace devops_pr_analyzer.shared.Models;

public class AnalysisRequest
{
    public EProgrammingLanguage Language { get; init; }
    public IReadOnlyCollection<AnalyzerFindings> Findings { get; init; } = [];

    public GitInfo? GitInfo { get; set; }
    public EGitProvider GitProvider { get; set; }

    public List<string> FileExclusions { get; set; } = [];

    public bool EnableSummaryComment { get; set; } = true;

    public bool EnableInlineSuggestions { get; set; } = true;

    public bool EnableDescriptionSummary { get; set; } = true;

    public bool EnableAzureDevopsCodeOwners { get; set; } = false;

    // Git provider credentials - configured per request
    public string? DevopsPat { get; set; }
    public string? AzureDevOpsOrgUrl { get; set; }
    public string? GitHubToken { get; set; }
}
