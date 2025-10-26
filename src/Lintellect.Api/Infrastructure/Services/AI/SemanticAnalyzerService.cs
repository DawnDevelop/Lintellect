using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text;
using System.Text.Json;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using Semantic Kernel (AIFoundry) for code analysis.
/// </summary>
public sealed class SemanticAnalyzerService(SemanticAnalyzerOptions options) : IAnalyzerService
{
    private readonly SemanticAnalyzerOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly PromptTemplateService _templateService = new();
    private readonly AnalysisPromptBuilder _promptBuilder = new();

    // <inheritdoc/>
    public async Task<string> AnalyzeAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        var kernel = CreateKernel(_options);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt
            });

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(_promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs));

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature,
            ResponseFormat = "text" // Use text for detailed markdown analysis
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken)
            ;

        return response.Content ?? "No analysis generated.";
    }

    // <inheritdoc/>
    public async Task<CodeOwnersResult?> GetCodeOwnersAsync(
        string codeOwnerFileContent,
        List<string> changedFilePaths,
        CancellationToken cancellationToken = default)
    {
        var kernel = CreateKernel(_options);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderTemplate(AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.CodeOwnerSystemPrompt]);

        var chatHistory = new ChatHistory(systemPrompt);

        // Create a message that includes both the CODEOWNERS content and the changed file paths
        var userMessage = $"""
            CODEOWNERS file content:
            {codeOwnerFileContent}

            Changed files in this pull request:
            {string.Join("\n", changedFilePaths.Select(path => $"- {path}"))}
            """;

        chatHistory.AddUserMessage(userMessage);

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = 0.2, // Lower temperature for more precise suggestions
            ResponseFormat = "json_object" // Request JSON output for structured parsing
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<CodeOwnersResult>(response.Content, JsonExtensions.JsonSerializerOptions);
        if (result is null)
            return null;

        return result;
    }

    // <inheritdoc/>
    public async Task<string> GenerateSummaryAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        var kernel = CreateKernel(_options);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt,
            analysisResult.AnalysisResult.Language);

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(_promptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs));

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = 500, // Keep summaries concise
            Temperature = 0.3 // Lower temperature for more focused summaries
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        return response.Content ?? "No summary generated.";
    }

    // <inheritdoc/>
    public async Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        var kernel = CreateKernel(_options);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["gitProvider"] = analysisResult.AnalysisResult.GitProvider.ToString(),
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt
            });

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(_promptBuilder.BuildInlineSuggestionsPrompt(analysisResult.AnalysisResult, diffs));

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = 0.2, // Lower temperature for more precise suggestions
            ResponseFormat = "json_object" // Request JSON output for structured parsing
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken)
            ;

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return [];
        }

        var result = JsonSerializer.Deserialize<InlineSuggestionsResponse>(response.Content, JsonExtensions.JsonSerializerOptions);
        if (result is null)
            return [];

        return result.Suggestions;
    }

    /// <summary>
    /// Creates a new Kernel instance with the specified options.
    /// This allows for per-request configuration in the future.
    /// </summary>
    private static Kernel CreateKernel(SemanticAnalyzerOptions options)
    {
        var builder = Kernel.CreateBuilder();

        if (options.Endpoint is null)
            throw new InvalidOperationException("Endpoint must be provided for SemanticAnalyzerService.");

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            builder.AddAzureOpenAIChatCompletion(
                     deploymentName: options.DeploymentName,
                     endpoint: options.Endpoint,
                     apiKey: options.ApiKey);
        }
        else
        {
            if (options.TokenCredential is null)
                throw new InvalidOperationException("Either ApiKey and Endpoint or TokenCredential must be provided for SemanticAnalyzerService.");

            builder.AddAzureOpenAIChatCompletion(
                     deploymentName: options.DeploymentName,
                     endpoint: options.Endpoint!,
                     options.TokenCredential);
        }

        return builder.Build();
    }
}
