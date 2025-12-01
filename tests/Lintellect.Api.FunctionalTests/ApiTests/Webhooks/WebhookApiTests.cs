using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.ApiTests.Webhooks;

public class WebhookApiTests : BaseTestFixture
{
    [Test]
    public async Task SubmitAzureDevOpsPrCommentWebhook_WithValidEvent_ReturnsAccepted()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();

        // Act
        var response = await Client.SubmitAzureDevOpsPrCommentWebhookAsync(@event);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrEmpty();
        json.ShouldContain("webhookId");
    }

    [Test]
    public async Task SubmitAzureDevOpsPrCommentWebhook_WithNonQuestionComment_ReturnsAccepted()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidNonQuestionCommentEvent();

        // Act
        var response = await Client.SubmitAzureDevOpsPrCommentWebhookAsync(@event);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrEmpty();
        json.ShouldContain("webhookId");
    }

    [Test]
    public async Task SubmitAzureDevOpsPrUpdateWebhook_WithValidEvent_ReturnsAccepted()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidUpdateEvent();

        // Act
        var response = await Client.SubmitAzureDevOpsPrUpdateWebhookAsync(@event);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrEmpty();
        json.ShouldContain("webhookId");
    }

    [Test]
    public async Task SubmitAzureDevOpsPrCommentWebhook_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidQuestionCommentEvent();
        var clientWithoutKey = WebApplicationFactory.CreateClient();

        // Act
        var response = await clientWithoutKey.SubmitAzureDevOpsPrCommentWebhookAsync(@event);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SubmitAzureDevOpsPrUpdateWebhook_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var @event = WebhookTestDataBuilder.ValidUpdateEvent();
        var clientWithoutKey = WebApplicationFactory.CreateClient();

        // Act
        var response = await clientWithoutKey.SubmitAzureDevOpsPrUpdateWebhookAsync(@event);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

