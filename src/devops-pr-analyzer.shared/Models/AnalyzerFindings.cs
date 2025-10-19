namespace devops_pr_analyzer.shared.Models;

public class AnalyzerFindings
{
    public string RuleId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string Severity { get; init; } = "Info";
}