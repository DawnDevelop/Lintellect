using Lintellect.Api.Domain.Enums;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Domain.Events;

/// <summary>
/// Event raised when a webhook is received.
/// </summary>
public sealed class WebhookEventReceivedEvent : BaseEvent
{
    public Guid WebhookId { get; init; }
    public EPullRequestEventType EventType { get; init; }
    public EGitProvider Provider { get; init; }
}

/// <summary>
/// Event raised when a webhook starts processing.
/// </summary>
public sealed class WebhookEventProcessingEvent : BaseEvent
{
    public Guid WebhookId { get; init; }
}

/// <summary>
/// Event raised when a webhook is successfully processed.
/// </summary>
public sealed class WebhookEventCompletedEvent : BaseEvent
{
    public Guid WebhookId { get; init; }

}

/// <summary>
/// Event raised when a webhook processing fails.
/// </summary>
public sealed class WebhookEventFailedEvent : BaseEvent
{
    public Guid WebhookId { get; init; }
    public string ErrorMessage { get; init; } = null!;
}
