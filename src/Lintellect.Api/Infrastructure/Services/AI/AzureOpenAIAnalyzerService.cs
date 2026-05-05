using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using the Microsoft Agent Framework over Azure OpenAI for code analysis.
/// </summary>
public sealed class AzureOpenAIAnalyzerService(AzureOpenAIAnalyzerOptions options, IMcpServiceResolver resolver, ILogger<AzureOpenAIAnalyzerService> logger) : IAnalyzerService
{
    private readonly AzureOpenAIAnalyzerOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly PromptTemplateService _templateService = new();
    private readonly PromptBuilder _promptBuilder = new();
    private readonly ILogger<AzureOpenAIAnalyzerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // <inheritdoc/>
    public async Task<string> GetDetailedAnalysisAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting detailed analysis for language {Language}. DiffCount={DiffCount} McpServers={McpServers}",
            analysisResult.AnalysisResult.Language,
            diffs.Count,
            analysisResult.AnalysisResult.McpServer?.Count ?? 0);

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["workItemContext"] = analysisResult.WorkItemContext
            });

        var agent = await CreateAgentAsync(_options, systemPrompt, analysisResult.AnalysisResult.McpServer);

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            MaxOutputTokens = _options.MaxTokens,
            Temperature = (float)_options.Temperature,
            AllowMultipleToolCalls = true,
            ResponseFormat = ChatResponseFormat.Text
        });

        var userPrompt = _promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs);
        var response = await agent.RunAsync(userPrompt, options: runOptions, cancellationToken: cancellationToken);

        var content = string.IsNullOrEmpty(response.Text) ? "No analysis generated." : response.Text;
        _logger.LogInformation("Detailed analysis generated. ContentLength={ContentLength}", content.Length);
        return content;
    }

    // <inheritdoc/>
    public async Task<CodeOwnersResult?> GetCodeOwnersAsync(
        string codeOwnerFileContent,
        List<string> changedFilePaths,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating CODEOWNERS suggestions. ChangedFiles={ChangedFiles}", changedFilePaths.Count);

        var systemPrompt = _templateService.RenderTemplate(AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.CodeOwnerSystemPrompt]);

        var agent = await CreateAgentAsync(_options, systemPrompt);

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            MaxOutputTokens = _options.MaxTokens,
            Temperature = (float)_options.Temperature,
            AllowMultipleToolCalls = true,
            ResponseFormat = ChatResponseFormat.Json
        });

        var userMessage = $"""
            CODEOWNERS file content:
            {codeOwnerFileContent}

            Changed files in this pull request:
            {string.Join("\n", changedFilePaths.Select(path => $"- {path}"))}
            """;

        var response = await agent.RunAsync(userMessage, options: runOptions, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            _logger.LogWarning("CODEOWNERS suggestions response was empty");
            return null;
        }

        var result = JsonSerializer.Deserialize<CodeOwnersResult>(response.Text, JsonExtensions.JsonSerializerOptions);
        if (result is null)
        {
            _logger.LogWarning("Failed to deserialize CODEOWNERS suggestions");
        }
        else
        {
            _logger.LogInformation("CODEOWNERS suggestions deserialized successfully");
        }
        return result;
    }

    // <inheritdoc/>
    public async Task<string> GenerateSummaryAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting summary generation for language {Language}. DiffCount={DiffCount}",
            analysisResult.AnalysisResult.Language,
            diffs.Count);

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["workItemContext"] = analysisResult.WorkItemContext
            });

        var agent = await CreateAgentAsync(_options, systemPrompt, analysisResult.AnalysisResult.McpServer);

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            MaxOutputTokens = 500,
            Temperature = (float)_options.Temperature,
            AllowMultipleToolCalls = true
        });

        var userPrompt = PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs);
        var response = await agent.RunAsync(userPrompt, options: runOptions, cancellationToken: cancellationToken);

        var content = string.IsNullOrEmpty(response.Text) ? "No summary generated." : response.Text;
        _logger.LogInformation("Summary generated. ContentLength={ContentLength}", content.Length);
        return content;
    }

    // <inheritdoc/>
    public async Task<string> AnswerQuestionAsync(
        AnalyzerServiceModel analysisResult,
        string threadContext,
        string question,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Answering question for PR. McpServers={McpServers}",
            analysisResult.AnalysisResult.McpServer?.Count ?? 0);

        var systemPrompt = _templateService.RenderTemplate(
            AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.QuestionAnsweringPrompt],
            new Dictionary<string, string>
            {
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["threadContext"] = threadContext
            });

        var agent = await CreateAgentAsync(_options, systemPrompt, analysisResult.AnalysisResult.McpServer);

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            MaxOutputTokens = _options.MaxTokens,
            Temperature = (float)_options.Temperature,
            AllowMultipleToolCalls = true,
            ResponseFormat = ChatResponseFormat.Text
        });

        var userPrompt = $"""
            this is my question:

            {question}
            """;
        var response = await agent.RunAsync(userPrompt, options: runOptions, cancellationToken: cancellationToken);

        var content = string.IsNullOrEmpty(response.Text) ? "No answer generated." : response.Text;
        _logger.LogInformation("Question answered. ContentLength={ContentLength}", content.Length);
        return content;
    }

    // <inheritdoc/>
    public async Task<string> SummarizeContextAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running context summarization. SystemLength={SystemLength} UserLength={UserLength} MaxTokens={MaxTokens}",
            systemPrompt.Length, userPrompt.Length, maxOutputTokens);

        var agent = await CreateAgentAsync(_options, systemPrompt);

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            MaxOutputTokens = maxOutputTokens,
            Temperature = (float)_options.Temperature,
            AllowMultipleToolCalls = false,
            ResponseFormat = ChatResponseFormat.Text
        });

        var response = await agent.RunAsync(userPrompt, options: runOptions, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    // <inheritdoc/>
    public async Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting inline suggestions for language {Language}. DiffCount={DiffCount} McpServers={McpServers}",
            analysisResult.AnalysisResult.Language,
            diffs.Count,
            analysisResult.AnalysisResult.McpServer?.Count ?? 0);

        if (diffs.Count == 0)
        {
            _logger.LogWarning("No diffs provided for inline suggestions");
            return [];
        }

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["gitProvider"] = analysisResult.AnalysisResult.GitProvider.ToString(),
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["mcpServers"] = analysisResult.AnalysisResult.McpServer is null ? "none" : string.Join(",", analysisResult.AnalysisResult.McpServer.Select(s => s.ToString())),
                ["totalFilesInPR"] = diffs.Count.ToString(),
                ["maxSuggestionsPerFile"] = ComputeMaxSuggestionsPerFile(diffs.Count, _options.MaxInlineSuggestions).ToString(),
                ["workItemContext"] = analysisResult.WorkItemGoal
            },
            enableGlobalInstructions: true);

        var agent = await CreateAgentAsync(_options, systemPrompt, analysisResult.AnalysisResult.McpServer);

        // Process files in parallel with concurrency limit to avoid rate limits
        // Azure OpenAI typically has rate limits (e.g., requests per minute), so we limit concurrency
        const int maxConcurrency = 5;
        var allSuggestions = new List<InlineSuggestion>();

        await Parallel.ForEachAsync(
            diffs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            },
            async (kvp, ct) =>
            {
                var (filePath, diff) = kvp;
                var suggestions = await ProcessFileForInlineSuggestionsAsync(
                    agent,
                    analysisResult.AnalysisResult,
                    filePath,
                    diff,
                    ct);

                if (suggestions is not null)
                {
                    lock (allSuggestions)
                    {
                        allSuggestions.AddRange(suggestions);
                    }
                }
            });

        _logger.LogInformation("Inline suggestions generated for all files. TotalCount={TotalCount}", allSuggestions.Count);

        return ApplyGlobalCap(allSuggestions, _options.MaxInlineSuggestions, _logger);
    }

    /// <summary>
    /// Applies a global cap to inline suggestions, selecting the highest-severity ones first.
    /// </summary>
    internal static List<InlineSuggestion> ApplyGlobalCap(List<InlineSuggestion> suggestions, int maxInlineSuggestions, ILogger? logger = null)
    {
        if (maxInlineSuggestions <= 0 || suggestions.Count <= maxInlineSuggestions)
        {
            return suggestions;
        }

        var capped = suggestions
            .OrderByDescending(s => s.Severity?.ToLowerInvariant() switch
            {
                "error" => 3,
                "warning" => 2,
                "info" => 1,
                _ => 0
            })
            .Take(maxInlineSuggestions)
            .ToList();

        logger?.LogInformation(
            "Inline suggestions capped from {Original} to {Capped} (MaxInlineSuggestions={Max})",
            suggestions.Count, capped.Count, maxInlineSuggestions);

        return capped;
    }

    /// <summary>
    /// Computes the per-file suggestion limit based on PR size and global cap.
    /// For small PRs the budget is generous; for large PRs it tightens automatically.
    /// </summary>
    internal static int ComputeMaxSuggestionsPerFile(int fileCount, int globalMax)
    {
        if (fileCount <= 0) return 5;
        var perFile = globalMax > 0 ? Math.Max(1, globalMax / fileCount) : 5;
        return Math.Min(perFile, 5); // never exceed 5 per file even for very small PRs
    }

    /// <summary>
    /// Processes a single file to generate inline suggestions.
    /// </summary>
    private async Task<List<InlineSuggestion>?> ProcessFileForInlineSuggestionsAsync(
        AIAgent agent,
        AnalysisRequest analysisResult,
        string filePath,
        string diff,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing inline suggestions for file {FilePath}", filePath);

            var userPrompt = _promptBuilder.BuildInlineSuggestionsPrompt(
                analysisResult,
                new Dictionary<string, string> { [filePath] = diff });

            var runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
                MaxOutputTokens = _options.MaxTokens,
                Temperature = (float)_options.Temperature,
                AllowMultipleToolCalls = true,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<InlineSuggestionsResponse>(JsonExtensions.JsonSerializerOptions)
            });

            var response = await agent.RunAsync(userPrompt, options: runOptions, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                _logger.LogWarning("Inline suggestions response was empty for file {FilePath}", filePath);
                return null;
            }

            var result = JsonSerializer.Deserialize<InlineSuggestionsResponse>(response.Text, JsonExtensions.JsonSerializerOptions);
            if (result is null)
            {
                _logger.LogWarning("Failed to deserialize inline suggestions for file {FilePath}", filePath);
                return null;
            }

            // Ensure all suggestions have the correct file path (in case AI didn't set it correctly)
            var correctedSuggestions = result.Suggestions.Select(suggestion =>
            {
                if (string.IsNullOrWhiteSpace(suggestion.FilePath) || !suggestion.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Correcting file path for suggestion from '{OriginalPath}' to '{CorrectPath}'", suggestion.FilePath, filePath);
                    return suggestion with { FilePath = filePath };
                }
                return suggestion;
            }).ToList();

            _logger.LogDebug("Generated {Count} inline suggestions for file {FilePath}", correctedSuggestions.Count, filePath);
            return correctedSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inline suggestions for file {FilePath}. Continuing with other files.", filePath);
            return null;
        }
    }

    /// <summary>
    /// Creates an Azure OpenAI–backed AIAgent with the given system prompt and (optional) MCP tools.
    /// </summary>
    private async Task<AIAgent> CreateAgentAsync(AzureOpenAIAnalyzerOptions options, string instructions, List<EMcpServer>? mcpServers = null)
    {
        _logger.LogDebug("Creating AIAgent for deployment {Deployment} at {Endpoint}", options.DeploymentName, options.Endpoint);

        if (options.Endpoint is null)
        {
            throw new InvalidOperationException("Endpoint must be provided for AzureOpenAIAnalyzerService.");
        }

        AzureOpenAIClient azureClient;
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogDebug("Using ApiKey authentication for Azure OpenAI");
            azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey));
        }
        else
        {
            if (options.TokenCredential is null)
            {
                throw new InvalidOperationException("Either ApiKey and Endpoint or TokenCredential must be provided for AzureOpenAIAnalyzerService.");
            }

            _logger.LogDebug("Using TokenCredential authentication for Azure OpenAI");
            azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), options.TokenCredential);
        }

        var tools = await CollectMcpToolsAsync(mcpServers);

        return azureClient
            .GetChatClient(options.DeploymentName!)
            .AsIChatClient()
            .AsAIAgent(instructions: instructions, tools: tools);
    }

    private async Task<List<AITool>> CollectMcpToolsAsync(List<EMcpServer>? mcpServers)
    {
        var tools = new List<AITool>();
        if (mcpServers is null || mcpServers.Count == 0)
        {
            _logger.LogDebug("No MCP servers configured. Returning agent without tools.");
            return tools;
        }

        _logger.LogInformation("Configuring {Count} MCP server(s) for tool calling", mcpServers.Count);
        foreach (var mcpServer in mcpServers)
        {
            _logger.LogDebug("Resolving MCP server config for {Server}", mcpServer);
            var config = resolver.GetMcpService(mcpServer)?.GetMcpConfig();

            if (config is null)
            {
                _logger.LogWarning("MCP server {Server} configuration not found. Skipping.", mcpServer);
                continue;
            }

            var serverTools = await McpToolsAsync(config);
            tools.AddRange(serverTools);
            _logger.LogInformation("Added {ToolCount} tool(s) from MCP server {ServerName}", serverTools.Count, config.Name);
        }

        _logger.LogInformation("Total MCP tools registered: {TotalTools}", tools.Count);
        return tools;
    }

    private static async Task<List<AITool>> McpToolsAsync(McpConfig config)
    {
        var httpTransport = new HttpClientTransport(new HttpClientTransportOptions()
        {
            Endpoint = new Uri(config.Url),
            Name = config.Name,
            TransportMode = HttpTransportMode.AutoDetect,
            AdditionalHeaders = config.AdditionalHeaders
        });
        var client = await McpClient.CreateAsync(httpTransport);
        var toolsList = await client.ListToolsAsync();
        return [.. toolsList.Cast<AITool>()];
    }
}
