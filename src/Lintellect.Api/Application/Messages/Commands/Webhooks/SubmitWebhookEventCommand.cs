using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Infrastructure.Services.Webhooks;
using Lintellect.Shared.Models;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands.Webhooks;

/// <summary>
/// Command to submit a new webhook event following CleanArchitecture pattern.
/// </summary>
public sealed record SubmitWebhookEventCommand(
    EPullRequestEventType EventType,
    EGitProvider Provider,
    object EventPayload) : IRequest<Guid>;

/// <summary>
/// Handler for SubmitWebhookEventCommand following CleanArchitecture pattern.
/// </summary>
public sealed class SubmitWebhookEventCommandHandler(
    IApplicationDbContext context,
    WebhookJobQueue queue) : IRequestHandler<SubmitWebhookEventCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var webhookEvent = new WebhookEvent(
            request.EventType,
            request.Provider,
            request.EventPayload);

        context.WebhookEvents.Add(webhookEvent);
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(webhookEvent, cancellationToken);

        return webhookEvent.Id;
    }
}

