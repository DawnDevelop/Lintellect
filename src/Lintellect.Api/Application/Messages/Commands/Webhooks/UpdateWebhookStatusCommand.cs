using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Commands.Webhooks;

/// <summary>
/// Command to update a webhook event status following CleanArchitecture pattern.
/// </summary>
public sealed record UpdateWebhookStatusCommand(
    Guid WebhookId,
    WebhookStatus Status,
    string? ErrorMessage = null) : IRequest;

/// <summary>
/// Handler for UpdateWebhookStatusCommand following CleanArchitecture pattern.
/// </summary>
public sealed class UpdateWebhookStatusCommandHandler(
    IApplicationDbContext context) : IRequestHandler<UpdateWebhookStatusCommand>
{
    public async ValueTask<Unit> Handle(UpdateWebhookStatusCommand request, CancellationToken cancellationToken)
    {
        var webhookEvent = await context.WebhookEvents
            .FirstOrDefaultAsync(w => w.Id == request.WebhookId, cancellationToken);

        if (webhookEvent is null)
        {
            throw new InvalidOperationException($"Webhook event {request.WebhookId} not found");
        }

        // Update status based on the requested status
        switch (request.Status)
        {
            case WebhookStatus.Processing:
                webhookEvent.StartProcessing();
                break;

            case WebhookStatus.Completed:
                webhookEvent.Complete();
                break;

            case WebhookStatus.Failed:
                webhookEvent.Fail(request.ErrorMessage ?? "Unknown error");
                break;

            default:
                throw new InvalidOperationException($"Cannot update to status {request.Status}");
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

