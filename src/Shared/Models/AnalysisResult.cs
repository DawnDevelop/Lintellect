namespace devops_pr_analyzer.shared.Models;

public class AnalysisResult
{
    public string Language { get; init; } = string.Empty;
    public IReadOnlyCollection<AnalyzerFindings> Findings { get; init; } = [];

    public GitInfo? GitInfo { get; set; }
    public EGitProvider GitProvider { get; set; }
}
