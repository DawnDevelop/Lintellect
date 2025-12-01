using Lintellect.Api.Application.Messages.Commands.Webhooks;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Webhooks;

public class SubmitWebhookEventCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithValidCommentEvent_CreatesWebhookEvent()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var command = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        // Act
        var webhookId = await SendAsync(command);

        // Assert
        webhookId.ShouldNotBe(Guid.Empty);

        var (scope, context) = GetDbContext();
        using (scope)
        {
            var webhookEvent = await context.WebhookEvents.FindAsync(webhookId);

            webhookEvent.ShouldNotBeNull();
            webhookEvent!.EventType.ShouldBe(EPullRequestEventType.CommentedOn);
            webhookEvent.Provider.ShouldBe(EGitProvider.AzureDevops);
            // Status may be Pending, Processing, or Completed depending on background service timing
            // The important thing is that the webhook was created and enqueued successfully
            webhookEvent.Status.ShouldBeOneOf(WebhookStatus.Pending, WebhookStatus.Processing, WebhookStatus.Completed);
        }
    }

    [Test]
    public async Task Handle_WithValidUpdateEvent_CreatesWebhookEvent()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidUpdateEvent();
        var command = new SubmitWebhookEventCommand(
            EPullRequestEventType.Updated,
            EGitProvider.AzureDevops,
            @event);

        // Act
        var webhookId = await SendAsync(command);

        // Assert
        webhookId.ShouldNotBe(Guid.Empty);

        var (scope, context) = GetDbContext();
        using (scope)
        {
            var webhookEvent = await context.WebhookEvents.FindAsync(webhookId);

            webhookEvent.ShouldNotBeNull();
            webhookEvent!.EventType.ShouldBe(EPullRequestEventType.Updated);
            webhookEvent.Provider.ShouldBe(EGitProvider.AzureDevops);
            // Status may be Pending, Processing, or Completed depending on background service timing
            // The important thing is that the webhook was created and enqueued successfully
            webhookEvent.Status.ShouldBeOneOf(WebhookStatus.Pending, WebhookStatus.Processing, WebhookStatus.Completed);
        }
    }
}

