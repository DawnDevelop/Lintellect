using Lintellect.Shared.Models;
using System.Text;
using System.Text.Json;

namespace Lintellect.Cli.Services;

internal class AnalyzerApiClientService(Uri baseUrl, string apiKey) : IDisposable
{
    private static Uri StartAnalysisEndpoint => new("api/analysis/start", UriKind.Relative);

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = baseUrl,
        DefaultRequestHeaders =
        {
            { "Api-Key", apiKey }
        }
    };

    public async Task<HttpResponseMessage> StartAnalysisAsync(
        AnalysisRequest result)
    {
        var request = new
        {
            AnalysisResult = result
        };

        var jsonContent = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            jsonContent,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(StartAnalysisEndpoint, content)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return response;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
