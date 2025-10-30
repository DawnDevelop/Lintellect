using Lintellect.Api.Application.Messages.Commands.Analysis;

namespace Lintellect.Api.functionaltests.Utilities;

/// <summary>
/// Extension methods for HttpClient to simplify API testing.
/// </summary>
public static class HttpClientExtensions
{
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

    public static async Task<T?> ReadAsJsonAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
