using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Batches;
using Anthropic.SDK.Messaging;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;
using Polly;

namespace Lintellect.Api.Infrastructure.Services.AI;

/// <summary>
/// Analyzer service using Anthropic Claude API for code analysis.
/// Implements IBatchAnalyzerService to support efficient batched operations.
/// </summary>
internal sealed class ClaudeAnalyzerService : IBatchAnalyzerService
{
    private readonly ClaudeAnalyzerOptions _options;
    private readonly PromptTemplateService _templateService;
    private readonly PromptBuilder _promptBuilder;
    private readonly AnthropicClient _client;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IMcpServiceResolver _mcpServiceResolver;

    public ClaudeAnalyzerService(ClaudeAnalyzerOptions options, IMcpServiceResolver mcpServiceResolver)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mcpServiceResolver = mcpServiceResolver ?? throw new ArgumentNullException(nameof(mcpServiceResolver));
        _templateService = new PromptTemplateService();
        _promptBuilder = new PromptBuilder();
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
    public async Task<string> GetDetailedAnalysisAsync(
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

            // Use optimized prompt builder with truncation and prioritization
            var userPrompt = _promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs);

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

            // Use optimized prompt builder with truncation, prioritization, and filtered findings
            var userPrompt = _promptBuilder.BuildInlineSuggestionsPrompt(analysisResult.AnalysisResult, diffs);

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


            var userPrompt = PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs);

            var response = await SendClaudeMessageAsync(systemPrompt, userPrompt, cancellationToken);

            return response;
        });
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
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemPrompt = _templateService.RenderTemplate(
                AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.QuestionAnsweringPrompt],
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                    { "threadContext", threadContext }
                });


            return await SendClaudeMessageAsync(systemPrompt, $"""
            this is my question:

            {question}
            """, cancellationToken);
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
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Build message parameters for each operation
            var detailedSystem = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.DetailedAnalysisSystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt }
                });

            var inlineSystem = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.InlineSuggestionsSystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string>
                {
                    { "customInstructions", analysisResult.CopilotInstructionsPrompt },
                    { "mcpServers", string.Join(",", analysisResult.AnalysisResult.McpServer ?? []) }
                }, true);

            var summarySystem = _templateService.RenderLanguageTemplate(
                LanguagePromptTemplates.SummarySystemPrompt,
                analysisResult.AnalysisResult.Language,
                new Dictionary<string, string> { { "customInstructions", analysisResult.CopilotInstructionsPrompt } });

            // Use optimized prompt builders with truncation and prioritization
            var analysisUser = _promptBuilder.BuildAnalysisPrompt(analysisResult.AnalysisResult, diffs);
            var inlineUser = _promptBuilder.BuildInlineSuggestionsPrompt(analysisResult.AnalysisResult, diffs);
            var summaryUser = PromptBuilder.BuildSummaryPrompt(analysisResult.AnalysisResult, diffs);

            var codeownersSystem = _templateService.RenderTemplate(AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.CodeOwnerSystemPrompt]);
            // Optimize CODEOWNERS prompt - more concise format
            var codeownersUser = $"CODEOWNERS:\n{codeOwnerFileContent}\n\nChanged files: {string.Join(", ", changedFilePaths)}";

            var commonMcp = BuildMcpServerConfigs(analysisResult.AnalysisResult.McpServer ?? []);

            var requests = new List<BatchRequest>();

            // Create batch requests based on enabled features
            var idDescriptionSummary = BuildDescriptionSummaryRequest(detailedSystem, analysisUser, commonMcp);
            var idInlineSuggestions = BuildInlineRequest(inlineSystem, inlineUser, commonMcp);
            var idSummary = BuildSummaryCommentRequest(summarySystem, summaryUser, commonMcp);
            var idCodeOwners = BuildCodeOwnersRequest(codeownersSystem, codeownersUser, commonMcp);


            var tokenCount = await _client.Messages.CountMessageTokensAsync(new()
            {
                Messages = idDescriptionSummary.MessageParameters.Messages,
                Tools = idDescriptionSummary.MessageParameters.Tools,
                Model = idDescriptionSummary.MessageParameters.Model,
                System = idDescriptionSummary.MessageParameters.System
            });

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

            // If no requests, return empty result
            if (requests.Count == 0)
            {
                return new BatchedAnalysisResult();
            }

            // Create batch
            var created = await _client.Batches.CreateBatchAsync(requests);

            // Poll until completed
            BatchResponse currentStatus;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                currentStatus = await _client.Batches.RetrieveBatchStatusAsync(created.Id, cancellationToken);
            } while (currentStatus.ProcessingStatus is "in_progress");

            // Retrieve results
            var detailed = string.Empty;
            var inlineRaw = string.Empty;
            var summary = string.Empty;
            var codeownersRaw = string.Empty;

            await foreach (var result in _client.Batches.RetrieveBatchResultsAsync(created.Id, cancellationToken))
            {
                if (result.Result.Type is "errored")
                {
                    continue;
                }

                detailed = result.CustomId == idDescriptionSummary.CustomId ? result.Result.Message.FirstMessage.Text ?? string.Empty : detailed;
                inlineRaw = result.CustomId == idInlineSuggestions.CustomId ? result.Result.Message.FirstMessage.Text ?? string.Empty : inlineRaw;
                summary = result.CustomId == idSummary.CustomId ? result.Result.Message.FirstMessage.Text ?? string.Empty : summary;
                codeownersRaw = result.CustomId == idCodeOwners.CustomId ? result.Result.Message.FirstMessage.Text ?? string.Empty : codeownersRaw;
            }

            // Parse inline suggestions
            List<InlineSuggestion> inline;
            try
            {
                inline = string.IsNullOrWhiteSpace(inlineRaw) ? [] : (JsonSerializer.Deserialize<List<InlineSuggestion>>(inlineRaw) ?? []);
            }
            catch
            {
                inline = [];
            }

            // Parse code owners
            CodeOwnersResult? codeowners;
            try
            {
                codeowners = string.IsNullOrWhiteSpace(codeownersRaw) ? null : JsonSerializer.Deserialize<CodeOwnersResult>(codeownersRaw);
            }
            catch
            {
                codeowners = null;
            }

            return new BatchedAnalysisResult
            {
                DetailedAnalysis = detailed,
                InlineSuggestions = inline,
                Summary = summary,
                CodeOwners = codeowners
            };
        });
    }

    /// <summary>
    /// Creates a MessageParameters object with common settings for batch requests.
    /// </summary>
    private MessageParameters CreateMessageParameters(string systemPrompt, string userPrompt, List<MCPServer>? mcpServers = null)
    {
        mcpServers ??= [];

        var messageParams = new MessageParameters
        {
            PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal?)_options.Temperature,
            Stream = false,
            ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
            Tools = [],
            System = [new(systemPrompt, new CacheControl { TTL = CacheDuration.OneHour })],
            Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = userPrompt }] }]
        };

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
            MessageParameters = CreateMessageParameters(codeownersSystem, codeownersUser, commonMcp)
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
            MessageParameters = CreateMessageParameters(inlineSystem, inlineUser, commonMcp)
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

    private async Task<string> SendClaudeMessageAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, List<EMcpServer>? mcpServers = null)
    {
        var mcpServerConfigs = BuildMcpServerConfigs(mcpServers ?? []);

        // For non-batch requests, we don't use PromptCaching
        var parameter = CreateMessageParameters(systemPrompt, userPrompt, mcpServerConfigs);
        parameter.PromptCaching = PromptCacheType.None; // Remove prompt caching for single messages

        var message = await _client.Messages.GetClaudeMessageAsync(parameter, cancellationToken);
        return message.ContentBlock?.Text ?? string.Empty;
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

    public Task<string> GetDetailedAnalysis(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
