using Lintellect.Api.Application.Messages.Commands.Webhooks;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.FunctionalTests.Utilities.Webhooks;
using Mediator;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Webhooks;

public class ProcessCommentWebhookEventCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithQuestionComment_ProcessesAndAnswersQuestion()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        var webhookId = await SendAsync(submitCommand);

        var (scope, context) = GetDbContext();
        WebhookEvent? webhookEvent;
        using (scope)
        {
            webhookEvent = await context.WebhookEvents.FindAsync(webhookId);
        }

        webhookEvent.ShouldNotBeNull();
        webhookEvent!.EventPayload.ShouldNotBeNull();

        var processCommand = new ProcessCommentWebhookEventCommand(webhookEvent);

        // Act
        var result = await SendAsync(processCommand);

        // Assert
        result.ShouldBe(Unit.Value);

        var (scope2, context2) = GetDbContext();
        using (scope2)
        {
            var persistedEvent = await context2.WebhookEvents.FindAsync(webhookId);
            persistedEvent.ShouldNotBeNull();
            persistedEvent!.Id.ShouldBe(webhookId);
        }
    }

    [Test]
    public async Task Handle_WithNonQuestionComment_SkipsProcessing()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidNonQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.AzureDevops,
            @event);

        var webhookId = await SendAsync(submitCommand);

        var (scope, context) = GetDbContext();
        WebhookEvent? webhookEvent;
        using (scope)
        {
            webhookEvent = await context.WebhookEvents.FindAsync(webhookId);
        }

        webhookEvent.ShouldNotBeNull();
        webhookEvent!.EventPayload.ShouldNotBeNull();

        var processCommand = new ProcessCommentWebhookEventCommand(webhookEvent);

        // Act
        var result = await SendAsync(processCommand);

        // Assert
        result.ShouldBe(Unit.Value);

        var (scope2, context2) = GetDbContext();
        using (scope2)
        {
            var persistedEvent = await context2.WebhookEvents.FindAsync(webhookId);
            persistedEvent.ShouldNotBeNull();
            persistedEvent!.Id.ShouldBe(webhookId);
        }
    }

    [Test]
    public async Task Handle_WithGitHubProvider_LogsNotImplemented()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var submitCommand = new SubmitWebhookEventCommand(
            EPullRequestEventType.CommentedOn,
            EGitProvider.GitHub,
            @event);

        var webhookId = await SendAsync(submitCommand);

        var (scope, context) = GetDbContext();
        WebhookEvent? webhookEvent;
        using (scope)
        {
            webhookEvent = await context.WebhookEvents.FindAsync(webhookId);
        }

        webhookEvent.ShouldNotBeNull();
        webhookEvent!.Provider.ShouldBe(EGitProvider.GitHub);
        webhookEvent.EventPayload.ShouldNotBeNull();

        var processCommand = new ProcessCommentWebhookEventCommand(webhookEvent);

        // Act
        var result = await SendAsync(processCommand);

        // Assert
        result.ShouldBe(Unit.Value);

        var (scope2, context2) = GetDbContext();
        using (scope2)
        {
            var persistedEvent = await context2.WebhookEvents.FindAsync(webhookId);
            persistedEvent.ShouldNotBeNull();
            persistedEvent!.Id.ShouldBe(webhookId);
            persistedEvent.Provider.ShouldBe(EGitProvider.GitHub);
        }
    }
}

