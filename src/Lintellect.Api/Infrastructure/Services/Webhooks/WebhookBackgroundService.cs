using Lintellect.Api.Application.Messages.Commands.Webhooks;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Domain.Enums;
using Mediator;

namespace Lintellect.Api.Infrastructure.Services.Webhooks;

/// <summary>
/// Background service that processes webhook events from the queue.
/// </summary>
public sealed class WebhookBackgroundService(
    WebhookJobQueue jobQueue,
    IServiceProvider serviceProvider,
    ILogger<WebhookBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook background service started");

        try
        {
            await foreach (var webhookEvent in jobQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessWebhookAsync(webhookEvent, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Webhook background service is stopping, cancelling webhook processing");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing webhook {WebhookId}", webhookEvent.Id);
                    // Try to update webhook status to failed
                    await TryUpdateWebhookStatusAsync(webhookEvent.Id, WebhookStatus.Failed, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Webhook background service stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in webhook background service");
        }
    }

    private async Task ProcessWebhookAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;

        logger.LogInformation("Processing webhook {WebhookId} of type {EventType} from {Provider}",
            webhookEvent.Id,
            webhookEvent.EventType,
            webhookEvent.Provider);

        // Create a scoped service provider for this webhook
        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Add webhook timeout (5 minutes)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            // Update webhook status to Processing using Mediator
            await mediator.Send(new UpdateWebhookStatusCommand(
                webhookEvent.Id,
                WebhookStatus.Processing),
                timeoutCts.Token);

            switch (webhookEvent.EventType)
            {
                case EPullRequestEventType.Created:
                    break;
                case EPullRequestEventType.Updated:
                    await mediator.Send(new ProcessUpdateWebhookEventCommand(webhookEvent), timeoutCts.Token);
                    break;
                case EPullRequestEventType.CommentedOn:
                    await mediator.Send(new ProcessCommentWebhookEventCommand(webhookEvent), timeoutCts.Token);
                    break;
                case EPullRequestEventType.Merged:
                    break;
                case EPullRequestEventType.Closed:
                    break;
            }

            // Update webhook status to Completed
            await mediator.Send(new UpdateWebhookStatusCommand(
                webhookEvent.Id,
                WebhookStatus.Completed),
                timeoutCts.Token);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
            logger.LogInformation("Successfully processed webhook {WebhookId} in {Duration}s",
                webhookEvent.Id, duration);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Webhook {WebhookId} timed out after 5 minutes", webhookEvent.Id);

            await mediator.Send(new UpdateWebhookStatusCommand(
                webhookEvent.Id,
                WebhookStatus.Failed,
                ErrorMessage: "Webhook processing timed out after 5 minutes"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process webhook {WebhookId}", webhookEvent.Id);

            // Update webhook status to Failed using Mediator
            await mediator.Send(new UpdateWebhookStatusCommand(
                webhookEvent.Id,
                WebhookStatus.Failed,
                ErrorMessage: ex.Message),
                cancellationToken);
        }
    }

    /// <summary>
    /// Attempts to update webhook status with error handling.
    /// </summary>
    private async Task TryUpdateWebhookStatusAsync(Guid webhookId, WebhookStatus status, string? errorMessage = null)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new UpdateWebhookStatusCommand(
                webhookId,
                status,
                ErrorMessage: errorMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update webhook {WebhookId} status to {Status}", webhookId, status);
        }
    }

    /// <summary>
    /// Graceful shutdown override.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping webhook background service, waiting for current webhooks...");
        await base.StopAsync(cancellationToken);
    }
}

