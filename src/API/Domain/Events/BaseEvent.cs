namespace devops_pr_analyzer.Domain.Events;

/// <summary>
/// Base domain event following CleanArchitecture pattern.
/// </summary>
public abstract class BaseEvent
{
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
}
