namespace Lintellect.Api.Domain.Events;

/// <summary>
/// Domain event raised when an analysis job completes successfully.
/// </summary>
public sealed class AnalysisJobCompletedEvent : BaseEvent
{
    public Guid JobId { get; }
    public string AnalyzerUsed { get; }

    public AnalysisJobCompletedEvent(Guid jobId, string analyzerUsed)
    {
        JobId = jobId;
        AnalyzerUsed = analyzerUsed;
    }
}
