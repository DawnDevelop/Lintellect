namespace Lintellect.Shared.Models;

public class AnalysisRequest
{
    public EProgrammingLanguage Language { get; set; }
    public List<AnalyzerFindings> Findings { get; set; } = [];

    public GitInfo? GitInfo { get; set; }
    public EGitProvider GitProvider { get; set; }

    public List<string> FileExclusions { get; set; } = [];

    public bool EnableSummaryComment { get; set; } = true;

    public bool EnableInlineSuggestions { get; set; } = true;

    public bool EnableDescriptionSummary { get; set; } = true;

    public bool EnableAzureDevopsCodeOwners { get; set; } = false;

    public bool EnableWorkItemContext { get; set; } = true;

    public List<WorkItemReference> WorkItems { get; set; } = [];

    public List<EMcpServer> McpServer { get; set; } = [];
}
