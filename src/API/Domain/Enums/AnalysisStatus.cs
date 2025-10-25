namespace devops_pr_analyzer.Domain.Enums;

/// <summary>
/// Represents the status of an analysis job.
/// </summary>
public enum AnalysisStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
