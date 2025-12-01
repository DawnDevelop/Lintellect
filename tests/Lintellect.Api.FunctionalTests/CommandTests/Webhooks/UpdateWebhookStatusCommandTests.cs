using Lintellect.Api.Application.Messages.Commands.Webhooks;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Webhooks;

public class UpdateWebhookStatusCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithPendingWebhook_UpdatesToProcessing()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        var webhookId = await SendAsync(submitCommand);

        var updateCommand = new UpdateWebhookStatusCommand(webhookId, WebhookStatus.Processing);

        // Act
        await SendAsync(updateCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var webhookEvent = await context.WebhookEvents.FindAsync(webhookId);

            webhookEvent.ShouldNotBeNull();
            webhookEvent!.Status.ShouldBe(WebhookStatus.Processing);
        }
    }

    [Test]
    public async Task Handle_WithProcessingWebhook_UpdatesToCompleted()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        var webhookId = await SendAsync(submitCommand);

        // First update to Processing
        var processingCommand = new UpdateWebhookStatusCommand(webhookId, WebhookStatus.Processing);
        await SendAsync(processingCommand);

        // Then update to Completed
        var completedCommand = new UpdateWebhookStatusCommand(webhookId, WebhookStatus.Completed);

        // Act
        await SendAsync(completedCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var webhookEvent = await context.WebhookEvents.FindAsync(webhookId);

            webhookEvent.ShouldNotBeNull();
            webhookEvent!.Status.ShouldBe(WebhookStatus.Completed);
        }
    }

    [Test]
    public async Task Handle_WithProcessingWebhook_UpdatesToFailed()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        var webhookId = await SendAsync(submitCommand);

        // First update to Processing
        var processingCommand = new UpdateWebhookStatusCommand(webhookId, WebhookStatus.Processing);
        await SendAsync(processingCommand);

        // Then update to Failed
        var failedCommand = new UpdateWebhookStatusCommand(webhookId, WebhookStatus.Failed, ErrorMessage: "Test error");

        // Act
        await SendAsync(failedCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var webhookEvent = await context.WebhookEvents.FindAsync(webhookId);

            webhookEvent.ShouldNotBeNull();
            webhookEvent!.Status.ShouldBe(WebhookStatus.Failed);
            webhookEvent.ErrorMessage.ShouldBe("Test error");
        }
    }

    [Test]
    public async Task Handle_WithNonExistentWebhook_ThrowsException()
    {
        // Arrange
        var nonExistentWebhookId = Guid.NewGuid();
        var updateCommand = new UpdateWebhookStatusCommand(nonExistentWebhookId, WebhookStatus.Processing);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(SendAsync(updateCommand));
    }
}

