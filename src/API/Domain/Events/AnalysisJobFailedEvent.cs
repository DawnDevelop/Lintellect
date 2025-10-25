namespace devops_pr_analyzer.Domain.Events;

/// <summary>
/// Domain event raised when an analysis job fails.
/// </summary>
public sealed class AnalysisJobFailedEvent : BaseEvent
{
    public Guid JobId { get; }
    public string ErrorMessage { get; }

    public AnalysisJobFailedEvent(Guid jobId, string errorMessage)
    {
        JobId = jobId;
        ErrorMessage = errorMessage;
    }
}
