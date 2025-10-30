using Lintellect.Api.Domain.Entities;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands.Webhooks;

// TODO Check if needed because CI pipeline should trigger a new analysis on PR updates anyways
public record ProcessUpdateWebhookEventCommand(WebhookEvent WebhookEvent) : IRequest;

public class ProcessUpdateWebhookEventCommandHandler(
    ILogger<ProcessWebhookEventCommandHandler> logger
    //,PullRequestService prService,
    //IAnalyzerServiceResolver aiResolver
    ) : IRequestHandler<ProcessUpdateWebhookEventCommand>
{

    async ValueTask<Unit> IRequestHandler<ProcessUpdateWebhookEventCommand, Unit>.Handle(ProcessUpdateWebhookEventCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing update webhook event: {WebhookEventId}", request.WebhookEvent.Id);

        throw new NotImplementedException();
    }
}
