using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Batches;
using Anthropic.SDK.Messaging;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Services;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;
using AIJsonSchemaCreateOptions = Microsoft.Extensions.AI.AIJsonSchemaCreateOptions;
using AIJsonSchemaTransformOptions = Microsoft.Extensions.AI.AIJsonSchemaTransformOptions;
using AIJsonUtilities = Microsoft.Extensions.AI.AIJsonUtilities;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using Anthropic Claude API for code analysis.
/// Implements IBatchAnalyzerService to support efficient batched operations.
/// </summary>
internal sealed class ClaudeAnalyzerService : IBatchAnalyzerService
{
    private static readonly TimeSpan BatchPollInterval = TimeSpan.FromSeconds(2);

    // Must stay below AnalysisBackgroundService.JobTimeout so a slow batch returns an empty
    // result (logged warning) instead of the whole job being cancelled by the job timeout.
    private static readonly TimeSpan BatchPollTimeout = TimeSpan.FromMinutes(50);

    // Same schema source as the Azure analyzer's ChatResponseFormat.ForJsonSchema, so Claude
    // JSON responses are schema-enforced instead of relying on prompt-format compliance.
    internal static readonly OutputFormat InlineSuggestionsOutputFormat = CreateOutputFormat<InlineSuggestionsResponse>();
    internal static readonly OutputFormat CodeOwnersOutputFormat = CreateOutputFormat<CodeOwnersResult>();

    private static OutputFormat CreateOutputFormat<T>()
    {
        return new()
        {
            // Anthropic structured outputs reject schemas whose objects don't set additionalProperties: false.
            Schema = AIJsonUtilities.CreateJsonSchema(
                typeof(T),
                serializerOptions: JsonExtensions.JsonSerializerOptions,
                inferenceOptions: new AIJsonSchemaCreateOptions
                {
                    TransformOptions = new AIJsonSchemaTransformOptions { DisallowAdditionalProperties = true }
                })
        };
    }

    private readonly ClaudeAnalyzerOptions _options;
    private readonly PromptTemplateService _templateService;
    private readonly PromptBuilder _promptBuilder;
    private readonly AnthropicClient _client;
    private readonly IMcpServiceResolver _mcpServiceResolver;
    private readonly ILogger<ClaudeAnalyzerService> _logger;

    public ClaudeAnalyzerService(
        ClaudeAnalyzerOptions options,
        IMcpServiceResolver mcpServiceResolver,
        IHttpClientFactory httpClientFactory,
        ILogger<ClaudeAnalyzerService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mcpServiceResolver = mcpServiceResolver ?? throw new ArgumentNullException(nameof(mcpServiceResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateService = new PromptTemplateService();
        _promptBuilder = new PromptBuilder();

        // The "ClaudeApi" HttpClient is registered in ConfigureServices.AddResiliencePolicies with retry + 10-minute timeout via Microsoft.Extensions.Http.Polly.
        var httpClient = httpClientFactory.CreateClient("ClaudeApi");
        _client = new AnthropicClient(new APIAuthentication(_options.ApiKey!), httpClient, requestInterceptor: null);
    }

    /// <summary>
    /// Analyzes code changes and provides detailed analysis.
    /// </summary>
    public async Task<string> GetDetailedAnalysisAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting detailed analysis for language {Language}. DiffCount={DiffCount}",
            analysisResult.AnalysisResult.Language, diffs.Count);

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "workItemContext", analysisResult.WorkItemContext }
            });

        var userPrompt = _promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs);

        var content = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken, analysisResult.AnalysisResult.McpServer);
        _logger.LogInformation("Detailed analysis generated. ContentLength={ContentLength}", content.Length);
        return content;
    }

    /// <summary>
    /// Generates inline code suggestions for PR comments.
    /// </summary>
    public async Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting inline suggestions for language {Language}. DiffCount={DiffCount}",
            analysisResult.AnalysisResult.Language, diffs.Count);

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "totalFilesInPR", diffs.Count.ToString() },
                { "maxSuggestionsPerFile", InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(diffs.Count, _options.MaxInlineSuggestions).ToString() },
                { "workItemContext", analysisResult.WorkItemGoal }
            });

        var userPrompt = _promptBuilder.BuildInlineSuggestionsPrompt(analysisResult.AnalysisResult, diffs);

        var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken, analysisResult.AnalysisResult.McpServer, InlineSuggestionsOutputFormat);

        try
        {
            var suggestions = ParseInlineSuggestions(response);
            return InlineSuggestionLimiter.ApplyGlobalCap(suggestions, _options.MaxInlineSuggestions, _logger);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize inline suggestions response from Claude. ResponseLength={ResponseLength}", response.Length);
            return [];
        }
    }

    /// <summary>
    /// Parses inline suggestions from a Claude response. Prefers the documented
    /// <c>{ "suggestions": [...] }</c> wrapper; falls back to a bare <c>[...]</c> array in case the
    /// model emits one anyway (unlike Azure, Claude output is not schema-enforced here).
    /// </summary>
    internal static List<InlineSuggestion> ParseInlineSuggestions(string raw)
    {
        try
        {
            // A successful object parse means it was the wrapper shape — trust its
            // (possibly empty) Suggestions rather than retrying as an array.
            var wrapped = JsonExtensions.DeserializeModelJson<InlineSuggestionsResponse>(raw);
            if (wrapped is not null)
            {
                return wrapped.Suggestions;
            }
        }
        catch (JsonException)
        {
            // Not a { "suggestions": [...] } object — fall through to the bare-array shape.
        }

        return JsonExtensions.DeserializeModelJson<List<InlineSuggestion>>(raw) ?? [];
    }

    /// <summary>
    /// Generates a concise summary of the analysis.
    /// </summary>
    public async Task<string> GenerateSummaryAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting summary generation for language {Language}. DiffCount={DiffCount}",
            analysisResult.AnalysisResult.Language, diffs.Count);

        var systemPrompt = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "workItemContext", analysisResult.WorkItemContext }
            });

        var userPrompt = PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs);

        var content = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken, analysisResult.AnalysisResult.McpServer);
        _logger.LogInformation("Summary generated. ContentLength={ContentLength}", content.Length);
        return content;
    }

    /// <summary>
    /// Answers a question about the pull request using the question-answering prompt template.
    /// </summary>
    public async Task<string> AnswerQuestionAsync(
        AnalyzerServiceModel analysisResult,
        string threadContext,
        string question,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Answering question for PR.");

        var systemPrompt = _templateService.RenderTemplate(
            AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.QuestionAnsweringPrompt],
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "threadContext", threadContext }
            });

        var content = await SendClaudeMessageAsync(systemPrompt, $"""
            this is my question:

            {question}
            """, cancellationToken, analysisResult.AnalysisResult.McpServer);

        _logger.LogInformation("Question answered. ContentLength={ContentLength}", content.Length);
        return content;
    }

    /// <summary>
    /// Analyzes CODEOWNERS file content and extracts structured code ownership information.
    /// </summary>
    public async Task<CodeOwnersResult?> GetCodeOwnersAsync(string codeOwnerFileContent, List<string> changedFilePaths, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating CODEOWNERS suggestions. ChangedFiles={ChangedFiles}", changedFilePaths.Count);

        var systemPrompt = _templateService.RenderTemplate("CodeOwnerSystemPrompt");
        var userPrompt = $"CODEOWNERS file content:\n{codeOwnerFileContent}\n\nChanged files:\n{string.Join("\n", changedFilePaths)}";

        var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken, outputFormat: CodeOwnersOutputFormat);

        try
        {
            return JsonExtensions.DeserializeModelJson<CodeOwnersResult>(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize CODEOWNERS response from Claude. ResponseLength={ResponseLength}", response.Length);
            return null;
        }
    }

    /// <summary>
    /// Executes the four analysis operations in a single Anthropic Messages Batch request.
    /// This is more efficient than making separate API calls.
    /// </summary>
    public async Task<BatchedAnalysisResult> RunBatchedAnalysisAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        string? codeOwnerFileContent,
        List<string> changedFilePaths,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batched analysis for language {Language}. DiffCount={DiffCount}",
            analysisResult.AnalysisResult.Language, diffs.Count);

        var detailedSystem = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "workItemContext", analysisResult.WorkItemContext }
            });

        var inlineSystem = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "mcpServers", string.Join(",", analysisResult.AnalysisResult.McpServer ?? []) },
                { "totalFilesInPR", diffs.Count.ToString() },
                { "maxSuggestionsPerFile", InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(diffs.Count, _options.MaxInlineSuggestions).ToString() },
                { "workItemContext", analysisResult.WorkItemGoal }
            }, true);

        var summarySystem = _templateService.RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt,
            analysisResult.AnalysisResult.Language,
            new Dictionary<string, string>
            {
                { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                { "workItemContext", analysisResult.WorkItemContext }
            });

        var analysisUser = _promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs);
        var inlineUser = _promptBuilder.BuildInlineSuggestionsPrompt(analysisResult.AnalysisResult, diffs);
        var summaryUser = PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs);

        var codeownersSystem = _templateService.RenderTemplate(AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.CodeOwnerSystemPrompt]);
        var codeownersUser = $"CODEOWNERS:\n{codeOwnerFileContent}\n\nChanged files: {string.Join(", ", changedFilePaths)}";

        var commonMcp = BuildMcpServerConfigs(analysisResult.AnalysisResult.McpServer ?? []);

        var requests = new List<BatchRequest>();

        var idDescriptionSummary = BuildDescriptionSummaryRequest(detailedSystem, analysisUser, commonMcp);
        var idInlineSuggestions = BuildInlineRequest(inlineSystem, inlineUser, commonMcp);
        var idSummary = BuildSummaryCommentRequest(summarySystem, summaryUser, commonMcp);
        var idCodeOwners = BuildCodeOwnersRequest(codeownersSystem, codeownersUser, commonMcp);

        //await _client.Messages.CountMessageTokensAsync(new()
        //{
        //    Messages = idDescriptionSummary.MessageParameters.Messages,
        //    Tools = idDescriptionSummary.MessageParameters.Tools,
        //    Model = idDescriptionSummary.MessageParameters.Model,
        //    System = idDescriptionSummary.MessageParameters.System
        //});

        // EnableSummaryComment = detailed analysis comment on PR
        if (analysisResult.AnalysisResult.EnableSummaryComment)
        {
            requests.Add(idDescriptionSummary);
        }

        if (analysisResult.AnalysisResult.EnableInlineSuggestions)
        {
            requests.Add(idInlineSuggestions);
        }

        // EnableDescriptionSummary = summary appended to PR description
        if (analysisResult.AnalysisResult.EnableDescriptionSummary)
        {
            requests.Add(idSummary);
        }

        if (analysisResult.AnalysisResult.EnableAzureDevopsCodeOwners && !string.IsNullOrWhiteSpace(codeOwnerFileContent))
        {
            requests.Add(idCodeOwners);
        }

        if (requests.Count == 0)
        {
            _logger.LogInformation("Batched analysis skipped: no operations enabled");
            return new BatchedAnalysisResult();
        }

        _logger.LogInformation("Submitting Claude batch with {RequestCount} request(s)", requests.Count);
        var created = await _client.Batches.CreateBatchAsync(requests, cancellationToken);

        // Poll until the batch reaches its terminal state ("ended"). Non-terminal states
        // ("in_progress", "canceling") must keep polling — exiting early would read results
        // from a batch that hasn't finished. A deadline guards against an unbounded loop.
        var deadline = DateTimeOffset.UtcNow + BatchPollTimeout;
        BatchResponse currentStatus;
        do
        {
            await Task.Delay(BatchPollInterval, cancellationToken);
            currentStatus = await _client.Batches.RetrieveBatchStatusAsync(created.Id, cancellationToken);
        } while (currentStatus.ProcessingStatus is not "ended" && DateTimeOffset.UtcNow < deadline);

        if (currentStatus.ProcessingStatus is not "ended")
        {
            _logger.LogWarning("Batch {BatchId} did not complete within {Timeout}; last status {Status}",
                created.Id, BatchPollTimeout, currentStatus.ProcessingStatus);
            return new BatchedAnalysisResult();
        }

        _logger.LogInformation("Batch {BatchId} completed with status {Status}", created.Id, currentStatus.ProcessingStatus);

        var detailed = string.Empty;
        var inlineRaw = string.Empty;
        var summary = string.Empty;
        var codeownersRaw = string.Empty;

        await foreach (var result in _client.Batches.RetrieveBatchResultsAsync(created.Id, cancellationToken))
        {
            if (result.Result.Type is "errored")
            {
                _logger.LogWarning("Batch result {CustomId} errored — skipping", result.CustomId);
                continue;
            }

            detailed = result.CustomId == idDescriptionSummary.CustomId ? ExtractText(result.Result.Message) : detailed;
            inlineRaw = result.CustomId == idInlineSuggestions.CustomId ? ExtractText(result.Result.Message) : inlineRaw;
            summary = result.CustomId == idSummary.CustomId ? ExtractText(result.Result.Message) : summary;
            codeownersRaw = result.CustomId == idCodeOwners.CustomId ? ExtractText(result.Result.Message) : codeownersRaw;
        }

        List<InlineSuggestion> inline;
        try
        {
            var parsed = string.IsNullOrWhiteSpace(inlineRaw) ? [] : ParseInlineSuggestions(inlineRaw);
            inline = InlineSuggestionLimiter.ApplyGlobalCap(parsed, _options.MaxInlineSuggestions, _logger);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize inline suggestions from batch result. RawLength={RawLength}", inlineRaw.Length);
            inline = [];
        }

        CodeOwnersResult? codeowners;
        try
        {
            codeowners = string.IsNullOrWhiteSpace(codeownersRaw) ? null : JsonExtensions.DeserializeModelJson<CodeOwnersResult>(codeownersRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize CODEOWNERS from batch result. RawLength={RawLength}", codeownersRaw.Length);
            codeowners = null;
        }

        return new BatchedAnalysisResult
        {
            DetailedAnalysis = detailed,
            InlineSuggestions = inline,
            Summary = summary,
            CodeOwners = codeowners
        };
    }

    /// <summary>
    /// Extracts the first text block from a message's content. Extended thinking places a "thinking"
    /// block before the "text" block, so the SDK's own <c>MessageResponse.FirstMessage</c> (which casts
    /// Content[0] unconditionally) returns null and cannot be used here.
    /// </summary>
    private static string ExtractText(MessageResponse message) =>
        message.Content?.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

    /// <summary>
    /// Creates a MessageParameters object with common settings for batch requests.
    /// </summary>
    private MessageParameters CreateMessageParameters(string systemPrompt, string userPrompt, List<MCPServer>? mcpServers = null, OutputFormat? outputFormat = null)
    {
        mcpServers ??= [];

        var messageParams = new MessageParameters
        {
            PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal?)_options.Temperature,
            Stream = false,
            OutputConfig = new OutputConfig { Effort = _options.Effort },
            ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
            Tools = [],
            Thinking = _options.Thinking is null ? null : new ThinkingParameters { Type = _options.Thinking.Value },
            System = [new(systemPrompt, new CacheControl { TTL = CacheDuration.OneHour })],
            Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = userPrompt }] }]
        };

        if (outputFormat is not null)
        {
            messageParams.OutputConfig.OutputFormat = outputFormat;
        }

        // Only set MCPServers if list is not empty (Claude API rejects empty lists)
        messageParams.MCPServers ??= [];
        foreach (var mcp in mcpServers)
        {
            messageParams.MCPServers.Add(new()
            {
                Name = mcp.Name,
                Url = mcp.Url,
                AuthorizationToken = mcp.AuthorizationToken,
                ToolConfiguration = new MCPToolConfiguration()
                {
                    Enabled = true,
                    AllowedTools = [$"{mcp.Name}"]
                }
            });
        }

        return messageParams;
    }

    private BatchRequest BuildCodeOwnersRequest(string codeownersSystem, string codeownersUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = CreateMessageParameters(codeownersSystem, codeownersUser, commonMcp, CodeOwnersOutputFormat)
        };
    }

    private BatchRequest BuildSummaryCommentRequest(string summarySystem, string summaryUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = CreateMessageParameters(summarySystem, summaryUser, commonMcp)
        };
    }

    private BatchRequest BuildInlineRequest(string inlineSystem, string inlineUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = CreateMessageParameters(inlineSystem, inlineUser, commonMcp, InlineSuggestionsOutputFormat)
        };
    }

    private BatchRequest BuildDescriptionSummaryRequest(string detailedSystem, string detailedUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = CreateMessageParameters(detailedSystem, detailedUser, commonMcp)
        };
    }

    private async Task<string> SendClaudeMessageAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, List<EMcpServer>? mcpServers = null, OutputFormat? outputFormat = null)
    {
        var mcpServerConfigs = BuildMcpServerConfigs(mcpServers ?? []);

        // For non-batch requests, we don't use PromptCaching
        var parameter = CreateMessageParameters(systemPrompt, userPrompt, mcpServerConfigs, outputFormat);
        parameter.PromptCaching = PromptCacheType.AutomaticToolsAndSystem;

        var message = await _client.Messages.GetClaudeMessageAsync(parameter, cancellationToken);

        return ExtractText(message);
    }

    private List<MCPServer> BuildMcpServerConfigs(List<EMcpServer> mcpServers)
    {
        var servers = new List<MCPServer>();
        foreach (var item in mcpServers)
        {
            var mcp = _mcpServiceResolver.GetMcpService(item);
            if (mcp is not null)
            {
                var config = mcp.GetMcpConfig();
                servers.Add(new()
                {
                    AuthorizationToken = config.AuthToken,
                    Name = config.Name,
                    ToolConfiguration = new MCPToolConfiguration()
                    {
                        Enabled = true,
                    },
                    Url = config.Url
                });
            }

        }

        return servers;
    }

}
