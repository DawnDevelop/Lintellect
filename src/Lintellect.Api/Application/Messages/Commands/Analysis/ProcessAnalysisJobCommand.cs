using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Extensions;
using Lintellect.Shared.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

/// <summary>
/// Command to process an analysis job following CleanArchitecture pattern.
/// </summary>
public sealed record ProcessAnalysisJobCommand(
    Guid JobId,
    AnalysisRequest AnalysisRequest) : IRequest<PullRequestAnalysisReportModel>;

/// <summary>
/// Handler for ProcessAnalysisJobCommand following CleanArchitecture pattern.
/// </summary>
public sealed class ProcessAnalysisJobCommandHandler(
    IApplicationDbContext context,
    PullRequestService prService,
    IAnalyzerService analyzerService,
    IWorkItemService workItemService,
    IWorkItemSummarizer workItemSummarizer,
    IOptions<AnalysisOptions> analysisOptions,
    ILogger<ProcessAnalysisJobCommandHandler> logger) : IRequestHandler<ProcessAnalysisJobCommand, PullRequestAnalysisReportModel>
{
    private readonly AnalysisOptions _analysisOptions = analysisOptions.Value;

    public async ValueTask<PullRequestAnalysisReportModel> Handle(ProcessAnalysisJobCommand request, CancellationToken cancellationToken)
    {
        var analysisRequest = request.AnalysisRequest;

        // Step 0: Check for duplicate analysis job with same PullRequestId and GitProvider
        if (await CheckForDuplicateAnalysisAsync(analysisRequest, cancellationToken))
        {
            return new PullRequestAnalysisReportModel
            {
                AnalysisResult = analysisRequest,
                Summary = "Duplicate analysis job detected. This pull request has already been analyzed.",
                DetailedAnalysis = string.Empty,
                DiffStatistics = new DiffStatistics
                {
                    FilesChanged = 0,
                    LinesAdded = 0,
                    LinesRemoved = 0
                },
                AnalyzedAt = DateTimeOffset.UtcNow,
                InlineSuggestions = null
            };
        }

        // Step 1: Get and filter diffs and findings

        var diffFull = await prService.GetCompactDiffsAsync(
            analysisRequest,
            contextLines: _analysisOptions.ContextLines);


        // Apply file exclusions if specified
        if (analysisRequest.FileExclusions != null && analysisRequest.FileExclusions.Count > 0)
        {
            var filteredFiles = FilePatternMatcher.FilterFiles(diffFull.Keys, analysisRequest.FileExclusions);
            diffFull = filteredFiles.ToDictionary(file => file, file => diffFull[file]);
        }

        // Filter findings to only include those for files that exist in diffs
        analysisRequest.Findings = [.. analysisRequest.Findings.Where(finding =>
            diffFull.ContainsKey(finding.FilePath))];


        // Step 2: Prepare analyzer and custom instructions
        var customInstructions = await prService.GetCustomInstructionsAsync(analysisRequest);

        // Step 2b: Resolve and summarize linked work items (graceful degradation on failure)
        var workItemSummary = await ResolveWorkItemContextAsync(analysisRequest, cancellationToken);

        var aiAnalyzerModel = new AnalyzerServiceModel(
            analysisRequest,
            customInstructions ?? string.Empty,
            WorkItemContext: workItemSummary.FullContext,
            WorkItemGoal: workItemSummary.Goal);

        // Step 3: Execute analysis tasks in parallel
        var analysisResults = await ExecuteAnalysisTasksAsync(analyzerService, aiAnalyzerModel, diffFull, analysisRequest, cancellationToken);

        // Step 4: Post results to PR
        await PostResultsToPullRequestAsync(prService, request.JobId, analysisRequest, analysisResults, cancellationToken);

        // Step 5: Return report
        return BuildAnalysisReport(analysisRequest, analysisResults, diffFull);
    }

    private async Task<AnalysisResults> ExecuteAnalysisTasksAsync(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {

        if (analyzer is IBatchAnalyzerService batchAnalyzer)
        {
            var codeOwnersContent = analysisRequest.EnableAzureDevopsCodeOwners
                ? await ResolveMatchingCodeOwnersAsync(analysisRequest, diffs.Keys)
                : null;

            var batchedResult = await batchAnalyzer.RunBatchedAnalysisAsync(
                aiAnalyzerModel,
                diffs,
                codeOwnersContent,
                [.. diffs.Keys],
                cancellationToken);

            return new AnalysisResults
            {
                Summary = batchedResult.Summary,
                DetailedAnalysis = batchedResult.DetailedAnalysis,
                InlineSuggestions = batchedResult.InlineSuggestions,
                CodeOwners = batchedResult.CodeOwners
            };
        }

        var tasks = new List<Task>();
        var diffPartial = await prService.GetCompactDiffsAsync(
            analysisRequest,
            contextLines: _analysisOptions.DetailedContextLines);

        var summaryTask = CreateSummaryTaskIfEnabled(analyzer, aiAnalyzerModel, diffPartial, analysisRequest, cancellationToken);
        var detailedAnalysisTask = CreateDetailedAnalysisTaskIfEnabled(analyzer, aiAnalyzerModel, diffPartial, analysisRequest, cancellationToken);
        var inlineSuggestionsTask = CreateInlineSuggestionsTaskIfEnabled(analyzer, aiAnalyzerModel, diffs, analysisRequest, cancellationToken);
        var codeOwnerTask = CreateCodeOwnerTaskIfEnabled(analyzer, analysisRequest, [.. diffs.Keys], cancellationToken);

        // Add non-null tasks to the list
        if (summaryTask is not null)
        {
            tasks.Add(summaryTask);
        }

        if (detailedAnalysisTask is not null)
        {
            tasks.Add(detailedAnalysisTask);
        }

        if (inlineSuggestionsTask is not null)
        {
            tasks.Add(inlineSuggestionsTask);
        }

        if (codeOwnerTask is not null)
        {
            tasks.Add(codeOwnerTask);
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Extract results
        return new AnalysisResults
        {
            Summary = summaryTask is not null ? await summaryTask : string.Empty,
            DetailedAnalysis = detailedAnalysisTask is not null ? await detailedAnalysisTask : string.Empty,
            InlineSuggestions = inlineSuggestionsTask is not null ? await inlineSuggestionsTask : [],
            CodeOwners = codeOwnerTask is not null ? await codeOwnerTask : null
        };
    }

    private static Task<string>? CreateSummaryTaskIfEnabled(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        return analysisRequest.EnableDescriptionSummary
            ? analyzer.GenerateSummaryAsync(aiAnalyzerModel, diffs, cancellationToken)
            : null;
    }

    private static Task<string>? CreateDetailedAnalysisTaskIfEnabled(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        return analysisRequest.EnableSummaryComment
            ? analyzer.GetDetailedAnalysisAsync(aiAnalyzerModel, diffs, cancellationToken)
            : null;
    }

    private static Task<List<InlineSuggestion>>? CreateInlineSuggestionsTaskIfEnabled(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        return analysisRequest.EnableInlineSuggestions
            ? analyzer.GenerateInlineSuggestionsAsync(aiAnalyzerModel, diffs, cancellationToken)
            : null;
    }

    private async Task<CodeOwnersResult?> CreateCodeOwnerTaskIfEnabled(
        IAnalyzerService analyzer,
        AnalysisRequest analysisRequest,
        List<string> changedFilePaths,
        CancellationToken cancellationToken)
    {
        if (!analysisRequest.EnableAzureDevopsCodeOwners)
        {
            return null;
        }

        var filtered = await ResolveMatchingCodeOwnersAsync(analysisRequest, changedFilePaths);
        if (filtered is null)
        {
            return null;
        }

        return await analyzer.GetCodeOwnersAsync(filtered, changedFilePaths, cancellationToken);
    }

    /// <summary>
    /// Fetches the CODEOWNERS file and returns only the lines whose rules match the changed
    /// paths, or <c>null</c> when the file is absent/empty or nothing matches — letting callers
    /// skip the LLM round-trip.
    /// </summary>
    private async Task<string?> ResolveMatchingCodeOwnersAsync(AnalysisRequest analysisRequest, IEnumerable<string> changedFilePaths)
    {
        var codeOwnersContent = await prService.GetCodeOwnersFileAsync(analysisRequest);
        if (string.IsNullOrWhiteSpace(codeOwnersContent))
        {
            return null;
        }

        var filtered = CodeOwnersPathFilter.FilterMatchingLines(codeOwnersContent, changedFilePaths);
        return string.IsNullOrEmpty(filtered) ? null : filtered;
    }

    private async Task PostResultsToPullRequestAsync(
        PullRequestService prService,
        Guid jobId,
        AnalysisRequest analysisRequest,
        AnalysisResults results,
        CancellationToken cancellationToken)
    {
        // Post detailed analysis comment
        if (!string.IsNullOrWhiteSpace(results.DetailedAnalysis) && analysisRequest.EnableSummaryComment)
        {
            var initialCommentThreadId = await GetInitialCommentThreadIdAsync(jobId, cancellationToken);

            await prService.AddCommentAsync(
                analysisRequest,
                results.DetailedAnalysis,
                threadId: initialCommentThreadId,
                isResolved: true);
        }

        // Append summary to description
        if (!string.IsNullOrWhiteSpace(results.Summary) && analysisRequest.EnableDescriptionSummary)
        {
            await prService.AppendToDescriptionAsync(analysisRequest, results.Summary);
        }

        // Post inline suggestions
        if (analysisRequest.EnableInlineSuggestions && results.InlineSuggestions.Count > 0)
        {
            await PostInlineSuggestionsAsync(prService, analysisRequest, results.InlineSuggestions);
        }

        // Add code owners
        if (analysisRequest.EnableAzureDevopsCodeOwners && results.CodeOwners?.CodeOwners.Count > 0)
        {
            await prService.AddCodeOwnersToPullRequest(
                analysisRequest,
                results.CodeOwners);
        }
    }

    private static async Task PostInlineSuggestionsAsync(
        PullRequestService prService,
        AnalysisRequest analysisRequest,
        List<InlineSuggestion> suggestions)
    {
        foreach (var suggestion in suggestions.Where(x => !string.IsNullOrWhiteSpace(x.SuggestedCode)))
        {
            var context = BuildSuggestionContext(suggestion);
            await prService.AddInlineSuggestionAsync(
                analysisRequest,
                suggestion.SuggestedCode,
                context,
                suggestion.FilePath,
                suggestion.LineFrom,
                suggestion.LineTo);
        }
    }

    private static PullRequestAnalysisReportModel BuildAnalysisReport(
        AnalysisRequest analysisRequest,
        AnalysisResults results,
        Dictionary<string, string> diffs)
    {
        return new PullRequestAnalysisReportModel
        {
            AnalysisResult = analysisRequest,
            Summary = results.Summary,
            DetailedAnalysis = results.DetailedAnalysis,
            DiffStatistics = BuildDiffStatistics(diffs),
            AnalyzedAt = DateTimeOffset.UtcNow,
            InlineSuggestions = results.InlineSuggestions.Count != 0 ? "Inline suggestions posted" : null
        };
    }

    private static string BuildSuggestionContext(InlineSuggestion suggestion)
    {
        var severityEmoji = suggestion.Severity?.ToLowerInvariant() switch
        {
            "error" => "🔴",
            "warning" => "🟡",
            "info" => "🔵",
            _ => "💡"
        };

        var context = $"{severityEmoji} **{suggestion.Title}**";

        if (!string.IsNullOrWhiteSpace(suggestion.RuleId))
        {
            context += $"\nRule: `{suggestion.RuleId}`";
        }

        context += $"\n\n{suggestion.Explanation}";

        return context;
    }

    private static DiffStatistics BuildDiffStatistics(Dictionary<string, string> diffs)
    {
        var filesChanged = diffs.Count;
        var linesAdded = 0;
        var linesRemoved = 0;

        foreach (var diff in diffs.Values)
        {
            var lines = diff.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                {
                    linesAdded++;
                }
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                {
                    linesRemoved++;
                }
            }
        }

        return new DiffStatistics
        {
            FilesChanged = filesChanged,
            LinesAdded = linesAdded,
            LinesRemoved = linesRemoved
        };
    }

    private async Task<WorkItemSummary> ResolveWorkItemContextAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken)
    {
        if (!analysisRequest.EnableWorkItemContext)
        {
            return WorkItemSummary.Empty;
        }

        try
        {
            var items = await workItemService.ResolveAsync(analysisRequest, cancellationToken);
            if (items.Count == 0)
            {
                logger.LogInformation("Work item context enabled but no linked items found for PR #{PullRequestId}",
                    analysisRequest.GitInfo?.PullRequestId);
                return WorkItemSummary.Empty;
            }

            return await workItemSummarizer.SummarizeAsync(items, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Work item context resolution failed for PR #{PullRequestId}; continuing without context",
                analysisRequest.GitInfo?.PullRequestId);
            return WorkItemSummary.Empty;
        }
    }

    private async Task<int?> GetInitialCommentThreadIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await context.AnalysisJobs
            .Where(job => job.Id == jobId)
            .Select(job => job.InitialCommentThreadId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> CheckForDuplicateAnalysisAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken)
    {

        var pullRequestId = analysisRequest.GitInfo!.PullRequestId;

        // Query for existing analysis jobs with the same PullRequestId and GitProvider
        var existingJob = await context.AnalysisJobs
            .Where(job =>
                job.AnalysisRequest != null &&
                job.Status == Domain.Enums.AnalysisStatus.Completed &&
                job.AnalysisRequest.GitInfo != null &&
                job.AnalysisRequest.GitInfo.PullRequestId == pullRequestId &&
                job.AnalysisRequest.GitProvider == analysisRequest.GitProvider)
            .OrderByDescending(job => job.Created)
            .FirstOrDefaultAsync(cancellationToken);

        return existingJob != null;

    }
}

/// <summary>
/// Container for analysis results from parallel tasks.
/// </summary>
internal sealed record AnalysisResults
{
    public string Summary { get; init; } = string.Empty;
    public string DetailedAnalysis { get; init; } = string.Empty;
    public List<InlineSuggestion> InlineSuggestions { get; init; } = [];
    public CodeOwnersResult? CodeOwners { get; init; }
}
