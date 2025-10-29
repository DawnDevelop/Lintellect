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

    private async Task HandlePullRequestCommentAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {


        // TODO: Implement PR comment handling logic
        // For example:
        // - Check if comment contains trigger keywords (e.g., "/analyze")
        // - Extract PR information
        // - Trigger analysis job
        // Example:
        // if (prCommentEvent.Resource?.Comment?.Content?.Contains("/analyze") == true)
        // {
        //     var analysisRequest = BuildAnalysisRequestFromComment(prCommentEvent);
        //     await mediator.Send(new SubmitAnalysisCommand(analysisRequest), cancellationToken);
        // }

        await Task.CompletedTask;
    }

    private async Task HandlePullRequestUpdatedAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var prUpdatedEvent = webhookEvent.EventPayload.Deserialize<PullRequestUpdatedEvent>();

        if (prUpdatedEvent is null)
        {
            logger.LogError("Failed to deserialize PullRequestUpdatedEvent for webhook {WebhookId}",
                webhookEvent.Id);
            return;
        }

        logger.LogInformation("Processing PR update for PR #{PullRequestId} in {Repository}",
            prUpdatedEvent.Resource?.PullRequestId,
            prUpdatedEvent.Resource?.Repository?.Name);

        // TODO: Implement PR update handling logic
        // For example:
        // - Check if PR has new commits
        // - Trigger automatic analysis on PR updates
        // - Update analysis status
        // Example:
        // if (prUpdatedEvent.Resource?.Status == "active")
        // {
        //     var analysisRequest = BuildAnalysisRequestFromPrUpdate(prUpdatedEvent);
        //     await mediator.Send(new SubmitAnalysisCommand(analysisRequest), cancellationToken);
        // }

        await Task.CompletedTask;
    }
}

