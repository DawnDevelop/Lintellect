using System.Text.Json;
using Lintellect.Api.Application.Models.Webhooks;
using Lintellect.Api.Domain.Entities;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands.Webhooks;

// TODO Figure out how to get AI Foundry or PAT to work with PR comments
public sealed record ProcessCommentWebhookEventCommand(WebhookEvent WebhookEvent) : IRequest;

/// <summary>
/// Processes webhook events and triggers appropriate actions.
/// </summary>
public sealed class ProcessWebhookEventCommandHandler(
    ILogger<ProcessWebhookEventCommandHandler> logger
    //,IAnalyzerServiceResolver analyzerResolver,
    //PullRequestService prservice
    ) : IRequestHandler<ProcessCommentWebhookEventCommand>
{
    public async ValueTask<Unit> Handle(ProcessCommentWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var prCommentEvent = request.WebhookEvent.EventPayload.Deserialize<PullRequestCommentEvent>();
        if (prCommentEvent is null)
        {
            logger.LogError("Failed to deserialize PullRequestCommentEvent for webhook {WebhookId}",
                request.WebhookEvent.Id);
            return Unit.Value;
        }

        logger.LogInformation("Processing PR comment on PR #{PullRequestId} in {Repository}",
            prCommentEvent.Resource?.PullRequestId,
            prCommentEvent.Resource?.Repository?.Name);





        return Unit.Value;
    }
}

