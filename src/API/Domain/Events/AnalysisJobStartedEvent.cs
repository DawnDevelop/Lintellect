namespace devops_pr_analyzer.Domain.Events;

/// <summary>
/// Domain event raised when an analysis job starts.
/// </summary>
public sealed class AnalysisJobStartedEvent : BaseEvent
{
    public Guid JobId { get; }

    public AnalysisJobStartedEvent(Guid jobId)
    {
        JobId = jobId;
    }
}
 