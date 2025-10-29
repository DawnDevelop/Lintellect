using Lintellect.Api.Apis.Authorization;
using Lintellect.Api.Application.Messages.Commands.Webhooks;
using Lintellect.Api.Application.Models.Webhooks;
using Lintellect.Api.Domain.Enums;
using Lintellect.Shared.Models;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lintellect.Api.Apis;

public static class AzureDevopsWebhooksApi
{
    public static IEndpointRouteBuilder MapAzureDevopsWebhooksApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/azuredevops/webhooks")
            .WithTags("Webhook")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        api.MapPost("/pr/commented-on", AzureDevopsPrCommentWebhook)
            .WithName("AzureDevopsPrCommentWebhook")
            .WithSummary("Receive Azure DevOps PR comment webhook")
            .WithDescription("Receives a webhook from Azure DevOps when a PR is commented on.");

        api.MapPost("/pr/updated", AzureDevopsPrUpdateWebhook)
            .WithName("AzureDevopsPrUpdateWebhook")
            .WithSummary("Receive Azure DevOps PR update webhook")
            .WithDescription("Receives a webhook from Azure DevOps when a PR is updated.");

        return app;
    }

    private static async Task<IResult> AzureDevopsPrCommentWebhook(
        [FromServices] IMediator mediator,
        [FromBody] PullRequestCommentEvent @event,
        CancellationToken cancellationToken)
    {
        var webhookId = await mediator.Send(
            new SubmitWebhookEventCommand(EPullRequestEventType.CommentedOn, EGitProvider.AzureDevops, @event),
            cancellationToken);

        return Results.Accepted($"/api/webhooks/{webhookId}", new { webhookId });
    }

    private static async Task<IResult> AzureDevopsPrUpdateWebhook(
        [FromServices] IMediator mediator,
        [FromBody] PullRequestUpdatedEvent @event,
        CancellationToken cancellationToken)
    {
        var webhookId = await mediator.Send(
            new SubmitWebhookEventCommand(EPullRequestEventType.Updated, EGitProvider.AzureDevops, @event),
            cancellationToken);

        return Results.Accepted($"/api/webhooks/{webhookId}", new { webhookId });
    }
}
