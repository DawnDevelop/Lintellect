using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Application.Models.Webhooks;
using Lintellect.Api.Infrastructure.Extensions;

namespace Lintellect.Api.FunctionalTests.Utilities.Http;

/// <summary>
/// Extension methods for HttpClient to simplify API testing.
/// </summary>
public static class HttpClientExtensions
{
    // Analysis API endpoints
    public static async Task<HttpResponseMessage> SubmitAnalysisAsync(
        this HttpClient client,
        SubmitAnalysisCommand command)
    {
        return await client.PostAsJsonAsync("/api/analysis/analyze", command);
    }

    public static async Task<HttpResponseMessage> GetAnalysisStatusAsync(
        this HttpClient client,
        Guid jobId)
    {
        return await client.GetAsync($"/api/analysis/status/{jobId}");
    }

    public static async Task<HttpResponseMessage> GetAnalysisHistoryAsync(
        this HttpClient client,
        int skip = 0,
        int take = 50,
        string? projectName = null,
        string? repositoryName = null)
    {
        var queryParams = new List<string>();
        if (skip > 0)
        {
            queryParams.Add($"skip={skip}");
        }

        if (take != 50)
        {
            queryParams.Add($"take={take}");
        }

        if (!string.IsNullOrEmpty(projectName))
        {
            queryParams.Add($"projectName={projectName}");
        }

        if (!string.IsNullOrEmpty(repositoryName))
        {
            queryParams.Add($"repositoryName={repositoryName}");
        }

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await client.GetAsync($"/api/analysis/history{query}");
    }

    public static async Task<HttpResponseMessage> DeleteAnalysisHistoryAsync(
        this HttpClient client,
        Guid? jobId = null)
    {
        var query = jobId.HasValue ? $"?jobId={jobId.Value}" : "";
        return await client.DeleteAsync($"/api/analysis/history{query}");
    }

    // Webhook API endpoints
    public static async Task<HttpResponseMessage> SubmitAzureDevOpsPrCommentWebhookAsync(
        this HttpClient client,
        PullRequestCommentEvent @event)
    {
        return await client.PostAsJsonAsync("/api/azuredevops/webhooks/pr/commented-on", @event);
    }

    public static async Task<HttpResponseMessage> SubmitAzureDevOpsPrUpdateWebhookAsync(
        this HttpClient client,
        PullRequestUpdatedEvent @event)
    {
        return await client.PostAsJsonAsync("/api/azuredevops/webhooks/pr/updated", @event);
    }

    // Common utilities
    public static async Task<T?> ReadAsJsonAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonExtensions.JsonSerializerOptions);
    }
}

