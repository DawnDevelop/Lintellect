using devops_pr_analyzer.shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace devops_pr_analyzer.cli.Services;

internal class AnalyzerApiClientService(Uri baseUrl, string apiKey) : IDisposable
{
    private static Uri AnalysisResultEndpoint => new ("api/analysis/result", UriKind.Relative);

    private readonly HttpClient _httpClient = new() {
        BaseAddress = baseUrl,
        DefaultRequestHeaders =
        {
            { "Api-Key", apiKey }
        }
    };

    public async Task<HttpResponseMessage> PostAnalysisResultAsync(AnalysisResult result)
    {
        var jsonContent = JsonSerializer.Serialize(result);

        using var content = new StringContent(
            jsonContent,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(AnalysisResultEndpoint, content)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return response;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
