using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;
using Polly;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using Anthropic Claude API for code analysis.
/// </summary>
internal sealed class ClaudeAnalyzerService : IAnalyzerService
{
    private readonly ClaudeAnalyzerOptions _options;
    private readonly PromptTemplateService _templateService;
    private readonly AnthropicClient _client;
    private readonly IAsyncPolicy _retryPolicy;

    public ClaudeAnalyzerService(ClaudeAnalyzerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _templateService = new PromptTemplateService();
        _client = new AnthropicClient(_options.ApiKey!);

        // Configure retry policy for Claude API calls
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) => Console.WriteLine($"Claude API retry {retryCount} in {timespan} seconds due to: {outcome?.Message}"));
    }

    /// <summary>
    /// Analyzes code changes and provides detailed analysis.
    /// </summary>
    public async Task<string> AnalyzeAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemPrompt = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt }
                });

            var userPrompt = BuildUserPrompt(diffs, analysisResult.AnalysisResult);

            return await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken);
        });
    }

    /// <summary>
    /// Generates inline code suggestions for PR comments.
    /// </summary>
    public async Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemPrompt = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt }
                });

            var userPrompt = BuildUserPrompt(diffs, analysisResult.AnalysisResult);

            var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken);

            try
            {
                var suggestions = JsonSerializer.Deserialize<List<InlineSuggestion>>(response);
                return suggestions ?? [];
            }
            catch
            {
                return [];
            }
        });
    }

    /// <summary>
    /// Generates a concise summary of the analysis.
    /// </summary>
    public async Task<string> GenerateSummaryAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemPrompt = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.SummarySystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt }
                });

            var userPrompt = BuildUserPrompt(diffs, analysisResult.AnalysisResult);

            var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken);

            return response;
        });
    }

    /// <summary>
    /// Analyzes CODEOWNERS file content and extracts structured code ownership information.
    /// </summary>
    public async Task<CodeOwnersResult?> GetCodeOwnersAsync(string codeOwnerFileContent, List<string> changedFilePaths, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemPrompt = _templateService.RenderTemplate("CodeOwnerSystemPrompt");
            var userPrompt = $"CODEOWNERS file content:\n{codeOwnerFileContent}\n\nChanged files:\n{string.Join("\n", changedFilePaths)}";

            var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken);

            try
            {
                var result = JsonSerializer.Deserialize<CodeOwnersResult>(response);
                return result;
            }
            catch
            {
                return null;
            }
        });
    }

    private async Task<string> SendClaudeMessageAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var parameter = new MessageParameters
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal?)_options.Temperature,
            Stream = false,
            System = [
                new(systemPrompt, new CacheControl(){
                        TTL = CacheDuration.OneHour
                    })
            ],
            Messages =
            [
                new()
                    {
                        Role = RoleType.User,
                        Content = [
                            new TextContent() {
                                Text = userPrompt
                            }
                        ]
                    }
            ]
        };

        var message = await _client.Messages.GetClaudeMessageAsync(parameter, cancellationToken);
        return message.ContentBlock?.Text ?? string.Empty;
    }

    /// <summary>
    /// Builds the user prompt from diffs and analysis result.
    /// </summary>
    private static string BuildUserPrompt(Dictionary<string, string> diffs, AnalysisRequest analysisRequest)
    {
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine($"Pull Request: {analysisRequest.GitInfo?.ProjectName}/{analysisRequest.GitInfo?.RepositoryName} #{analysisRequest.GitInfo?.PullRequestId}");
        prompt.AppendLine($"Language: {analysisRequest.Language}");
        prompt.AppendLine();

        foreach (var (filePath, diff) in diffs)
        {
            prompt.AppendLine($"File: {filePath}");
            prompt.AppendLine("```diff");
            prompt.AppendLine(diff);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }

        return prompt.ToString();
    }
}
