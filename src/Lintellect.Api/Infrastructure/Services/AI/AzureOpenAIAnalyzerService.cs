using System.Text.Json;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using Semantic Kernel (AIFoundry) for code analysis.
/// </summary>
public sealed class SemanticAnalyzerService(SemanticAnalyzerOptions options, IMcpServiceResolver resolver, ILogger<SemanticAnalyzerService> logger) : IAnalyzerService
{
    private readonly SemanticAnalyzerOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly PromptTemplateService _templateService = new();
    private readonly PromptBuilder _promptBuilder = new();
    private readonly ILogger<SemanticAnalyzerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static FunctionChoiceBehavior FunctionChoiceBehavior => FunctionChoiceBehavior.Auto(options: new() { AllowParallelCalls = true, AllowConcurrentInvocation = true });

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

        var kernel = await CreateKernelAsync(_options, analysisResult.AnalysisResult.McpServer);
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

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature,
            FunctionChoiceBehavior = FunctionChoiceBehavior,
            SetNewMaxCompletionTokensEnabled = true,
            ResponseFormat = "text" // Use text for detailed markdown analysis
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        var content = response.Content ?? "No analysis generated.";
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

        var kernel = await CreateKernelAsync(_options);
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

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature, // Lower temperature for more precise suggestions
            FunctionChoiceBehavior = FunctionChoiceBehavior,
            SetNewMaxCompletionTokensEnabled = true,
            ResponseFormat = "json_object" // Request JSON output for structured parsing
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("CODEOWNERS suggestions response was empty");
            return null;
        }

        var result = JsonSerializer.Deserialize<CodeOwnersResult>(response.Content, JsonExtensions.JsonSerializerOptions);
        if (result is null)
        {
            _logger.LogWarning("Failed to deserialize CODEOWNERS suggestions");
        }
        else
        {
            _logger.LogInformation("CODEOWNERS suggestions deserialized successfully");
        }
        return result is null ? null : result;
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

        var kernel = await CreateKernelAsync(_options, analysisResult.AnalysisResult.McpServer);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt,
            analysisResult.AnalysisResult.Language);

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs));

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = 500, // Keep summaries concise
            Temperature = _options.Temperature, // Lower temperature for more focused summaries
            SetNewMaxCompletionTokensEnabled = true,
            FunctionChoiceBehavior = FunctionChoiceBehavior
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        var content = response.Content ?? "No summary generated.";
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

        var kernel = await CreateKernelAsync(_options, analysisResult.AnalysisResult.McpServer);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderTemplate(
            AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.QuestionAnsweringPrompt],
            new Dictionary<string, string>
            {
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["threadContext"] = threadContext
            });

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage($"""
            this is my question:

            {question}
            """);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature,
            FunctionChoiceBehavior = FunctionChoiceBehavior,
            SetNewMaxCompletionTokensEnabled = true,
            ResponseFormat = "text" // Use text for markdown answers
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: kernel,
            cancellationToken: cancellationToken);

        var content = response.Content ?? "No answer generated.";
        _logger.LogInformation("Question answered. ContentLength={ContentLength}", content.Length);
        return content;
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

        var kernel = await CreateKernelAsync(_options, analysisResult.AnalysisResult.McpServer);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                ["gitProvider"] = analysisResult.AnalysisResult.GitProvider.ToString(),
                ["customInstructions"] = analysisResult.CopilotInstructionsPrompt,
                ["mcpServers"] = analysisResult.AnalysisResult.McpServer is null ? "none" : string.Join(",", analysisResult.AnalysisResult.McpServer.Select(s => s.ToString())),
                ["totalFilesInPR"] = diffs.Count.ToString(),
                ["maxSuggestionsPerFile"] = ComputeMaxSuggestionsPerFile(diffs.Count, _options.MaxInlineSuggestions).ToString()
            },
            enableGlobalInstructions: true);

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
                    chatCompletionService,
                    kernel,
                    systemPrompt,
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
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        string systemPrompt,
        AnalysisRequest analysisResult,
        string filePath,
        string diff,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing inline suggestions for file {FilePath}", filePath);

            var chatHistory = new ChatHistory();
            var userPrompt = _promptBuilder.BuildInlineSuggestionsPrompt(
                analysisResult,
                new Dictionary<string, string> { [filePath] = diff });
            chatHistory.AddUserMessage(userPrompt);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = _options.MaxTokens,
                SetNewMaxCompletionTokensEnabled = true,
                Temperature = _options.Temperature,
                FunctionChoiceBehavior = FunctionChoiceBehavior,
                ChatSystemPrompt = systemPrompt,
                ResponseFormat = typeof(InlineSuggestionsResponse) // Request JSON output for structured parsing
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: kernel,
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("Inline suggestions response was empty for file {FilePath}", filePath);
                return null;
            }

            var result = JsonSerializer.Deserialize<InlineSuggestionsResponse>(response.Content, JsonExtensions.JsonSerializerOptions);
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
                    // Create a new suggestion with the correct file path
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
    /// Creates a new Kernel instance with the specified options.
    /// This allows for per-request configuration in the future.
    /// </summary>
    private async Task<Kernel> CreateKernelAsync(SemanticAnalyzerOptions options, List<EMcpServer>? mcpServers = null)
    {
        _logger.LogDebug("Creating Kernel for deployment {Deployment} at {Endpoint}", options.DeploymentName, options.Endpoint);
        var builder = Kernel.CreateBuilder();

        if (options.Endpoint is null)
        {
            throw new InvalidOperationException("Endpoint must be provided for SemanticAnalyzerService.");
        }

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {


            _logger.LogDebug("Using ApiKey authentication for Azure OpenAI chat completion");
            builder.AddAzureOpenAIChatCompletion(
                     deploymentName: options.DeploymentName!,
                     endpoint: options.Endpoint,
                     apiKey: options.ApiKey);
        }
        else
        {
            if (options.TokenCredential is null)
            {
                throw new InvalidOperationException("Either ApiKey and Endpoint or TokenCredential must be provided for SemanticAnalyzerService.");
            }

            _logger.LogDebug("Using TokenCredential authentication for Azure OpenAI chat completion");
            builder.AddAzureOpenAIChatCompletion(
                     deploymentName: options.DeploymentName!,
                     endpoint: options.Endpoint!,
                     options.TokenCredential);
        }

        var chatCompletionService = builder.Build();

        if (mcpServers is null || mcpServers.Count == 0)
        {
            _logger.LogDebug("No MCP servers configured. Returning Kernel without tools.");
            return chatCompletionService;
        }

        _logger.LogInformation("Configuring {Count} MCP server(s) for tool calling", mcpServers.Count);
        var totalTools = 0;
        foreach (var mcpServer in mcpServers)
        {
            _logger.LogDebug("Resolving MCP server config for {Server}", mcpServer);
            var config = resolver.GetMcpService(mcpServer)?.GetMcpConfig();

            if (config is null)
            {
                _logger.LogWarning("MCP server {Server} configuration not found. Skipping.", mcpServer);
                continue;
            }

            var tools = await McpFunctionToolsAsync(config);

            chatCompletionService.Plugins.AddFromFunctions(config.Name, tools);
            _logger.LogInformation("Added {ToolCount} tool(s) from MCP server {ServerName}", tools.Count(), config.Name);
            totalTools += tools.Count();
        }

        _logger.LogInformation("Total MCP tools registered: {TotalTools}", totalTools);
        return chatCompletionService;
    }


    private static async Task<IEnumerable<KernelFunction>> McpFunctionToolsAsync(McpConfig config)
    {
        List<KernelFunction> tools = [];

        var httpTransport = new HttpClientTransport(new HttpClientTransportOptions()
        {
            Endpoint = new Uri(config.Url),
            Name = config.Name,
            TransportMode = HttpTransportMode.AutoDetect,
            AdditionalHeaders = config.AdditionalHeaders
        });
        var client = await McpClient.CreateAsync(httpTransport);
        var toolsList = await client.ListToolsAsync();
        foreach (var tool in toolsList)
        {
            tools.Add(tool.AsKernelFunction());
        }

        return tools;
    }
}
