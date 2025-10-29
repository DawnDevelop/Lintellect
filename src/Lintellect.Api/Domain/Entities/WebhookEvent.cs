using System.Text.Json;
using Lintellect.Api.Domain.Common;
using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Domain.Events;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Domain.Entities;

/// <summary>
/// Represents a webhook event received from a DevOps platform.
/// </summary>
public sealed class WebhookEvent : BaseAuditableEntity
{
    public EPullRequestEventType EventType { get; private set; } // "PR_Comment", "PR_Updated"
    public EGitProvider Provider { get; private set; }
    public JsonDocument EventPayload { get; private set; } = null!;
    public WebhookStatus Status { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Parameterless constructor for EF Core
    private WebhookEvent() { }

    public WebhookEvent(EPullRequestEventType eventType, EGitProvider provider, object eventPayload)
    {
        EventType = eventType;
        Provider = provider;
        EventPayload = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(eventPayload));
        Status = WebhookStatus.Pending;
        ReceivedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new WebhookEventReceivedEvent()
        {
            WebhookId = Id,
            EventType = eventType,
            Provider = provider,
            OccurredOn = DateTimeOffset.UtcNow
        });
    }

    public void StartProcessing()
    {
        if (Status != WebhookStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start processing webhook in {Status} status");
        }

        Status = WebhookStatus.Processing;
        AddDomainEvent(new WebhookEventProcessingEvent()
        {
            WebhookId = Id,
            OccurredOn = DateTimeOffset.UtcNow
        });
    }

    public void Complete()
    {
        if (Status != WebhookStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete webhook in {Status} status");
        }

        Status = WebhookStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new WebhookEventCompletedEvent()
        {
            WebhookId = Id,
            OccurredOn = DateTimeOffset.UtcNow
        });
    }

    public void Fail(string errorMessage)
    {
        if (Status is not WebhookStatus.Processing and not WebhookStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail webhook in {Status} status");
        }

        Status = WebhookStatus.Failed;
        ProcessedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;

        AddDomainEvent(new WebhookEventFailedEvent()
        {
            WebhookId = Id,
            ErrorMessage = errorMessage,
            OccurredOn = DateTimeOffset.UtcNow
        });
    }
}

