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
    private readonly AnthropicClient _client;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IMcpServiceResolver _mcpServiceResolver;

    public ClaudeAnalyzerService(ClaudeAnalyzerOptions options, IMcpServiceResolver mcpServiceResolver)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mcpServiceResolver = mcpServiceResolver ?? throw new ArgumentNullException(nameof(mcpServiceResolver));
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
                new Dictionary<string, string> { { "customInstructions", analysisResult.CopilotInstructionsPrompt } });

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

            var analysisUser = BuildUserPrompt(diffs, analysisResult.AnalysisResult);

            var codeownersSystem = _templateService.RenderTemplate("CodeOwnerSystemPrompt");
            var codeownersUser = $"CODEOWNERS file content:\n{codeOwnerFileContent}\n\nChanged files:\n{string.Join("\n", changedFilePaths)}";

            var commonMcp = BuildMcpServerConfigs(analysisResult.AnalysisResult.McpServer ?? []);

            var requests = new List<BatchRequest>();

            // Create batch requests based on enabled features
            var idDetailedAnalysis = BuildDescriptionSummaryRequest(detailedSystem, analysisUser, commonMcp);
            var idInlineSuggestions = BuildInlineRequest(inlineSystem, analysisUser, commonMcp);
            var idSummary = BuildSummaryCommentRequest(summarySystem, analysisUser, commonMcp);
            var idCodeOwners = BuildCodeOwnersRequest(codeownersSystem, codeownersUser, commonMcp);

            // EnableSummaryComment = detailed analysis comment on PR
            if (analysisResult.AnalysisResult.EnableSummaryComment)
            {
                requests.Add(idDetailedAnalysis);
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

                detailed = result.CustomId == idDetailedAnalysis.CustomId ? result.Result.Message.ContentBlock?.Text ?? string.Empty : detailed;
                inlineRaw = result.CustomId == idInlineSuggestions.CustomId ? result.Result.Message.ContentBlock?.Text ?? string.Empty : inlineRaw;
                summary = result.CustomId == idSummary.CustomId ? result.Result.Message.ContentBlock?.Text ?? string.Empty : summary;
                codeownersRaw = result.CustomId == idCodeOwners.CustomId ? result.Result.Message.ContentBlock?.Text ?? string.Empty : codeownersRaw;
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

    private BatchRequest BuildCodeOwnersRequest(string codeownersSystem, string codeownersUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = new MessageParameters
            {
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal?)_options.Temperature,
                Stream = false,
                MCPServers = commonMcp,
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
                System = [new(codeownersSystem, new CacheControl { TTL = CacheDuration.OneHour })],
                Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = codeownersUser }] }]
            }
        };
    }

    private BatchRequest BuildSummaryCommentRequest(string summarySystem, string summaryUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = new MessageParameters
            {
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal?)_options.Temperature,
                Stream = false,
                MCPServers = commonMcp,
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
                System = [new(summarySystem, new CacheControl { TTL = CacheDuration.OneHour })],
                Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = summaryUser }] }]
            }
        };
    }

    private BatchRequest BuildInlineRequest(string inlineSystem, string inlineUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = new MessageParameters
            {
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal?)_options.Temperature,
                Stream = false,
                MCPServers = commonMcp,
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
                System = [new(inlineSystem, new CacheControl { TTL = CacheDuration.OneHour })],
                Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = inlineUser }] }]
            },
        };
    }

    private BatchRequest BuildDescriptionSummaryRequest(string detailedSystem, string detailedUser, List<MCPServer> commonMcp)
    {
        return new()
        {
            CustomId = Guid.NewGuid().ToString(),
            MessageParameters = new MessageParameters
            {
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal?)_options.Temperature,
                Stream = false,
                MCPServers = commonMcp,
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto },
                System = [new(detailedSystem, new CacheControl { TTL = CacheDuration.OneHour })],
                Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = detailedUser }] }],
            }
        };
    }

    private async Task<string> SendClaudeMessageAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken, List<EMcpServer>? mcpServers = null)
    {
        var parameter = new MessageParameters
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal?)_options.Temperature,
            Stream = false,
            MCPServers = BuildMcpServerConfigs(mcpServers ?? []),
            ToolChoice = new ToolChoice()
            {
                Type = ToolChoiceType.Auto
            },
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

    public Task<string> GetDetailedAnalysis(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
