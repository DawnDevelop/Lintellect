namespace Lintellect.Api.Domain.Events;

/// <summary>
/// Domain event raised when an analysis job is created.
/// </summary>
public sealed class AnalysisJobCreatedEvent : BaseEvent
{
    public Guid JobId { get; }
    public string ProjectName { get; }
    public string RepositoryName { get; }
    public int PullRequestId { get; }

    public AnalysisJobCreatedEvent(Guid jobId, string projectName, string repositoryName, int pullRequestId)
    {
        JobId = jobId;
        ProjectName = projectName;
        RepositoryName = repositoryName;
        PullRequestId = pullRequestId;
    }
}
