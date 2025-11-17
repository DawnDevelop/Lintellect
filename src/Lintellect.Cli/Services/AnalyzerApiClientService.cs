using System.Text;
using System.Text.Json;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services;

internal class AnalyzerApiClientService : IDisposable
{
    private static Uri StartAnalysisEndpoint => new("api/analysis/analyze", UriKind.Relative);

    private readonly HttpClient _httpClient;

    public AnalyzerApiClientService(Uri baseUrl, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(apiKey);

        _httpClient = new HttpClient()
        {
            BaseAddress = baseUrl,
            DefaultRequestHeaders =
            {
                { "Api-Key", apiKey }
            }
        };
    }

    public async Task<HttpResponseMessage> StartAnalysisAsync(
        AnalysisRequest result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var request = new
        {
            AnalysisRequest = result
        };

        var jsonContent = JsonSerializer.Serialize(request);

        Console.WriteLine($"""
            Sending Post request to {_httpClient.BaseAddress}{StartAnalysisEndpoint}:

            {jsonContent}
            """);

        using StringContent content = new(
            jsonContent,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(StartAnalysisEndpoint, content)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
        return response;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
