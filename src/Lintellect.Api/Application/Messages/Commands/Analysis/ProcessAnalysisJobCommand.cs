using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Services;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Extensions;
using Lintellect.Shared.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

/// <summary>
/// Command to process an analysis job following CleanArchitecture pattern.
/// When <paramref name="ReanalysisBaseCommitId"/> is set, the job is an incremental re-analysis
/// that only reviews changes between that commit and <paramref name="SourceCommitId"/>.
/// </summary>
public sealed record ProcessAnalysisJobCommand(
    Guid JobId,
    AnalysisRequest AnalysisRequest,
    string? SourceCommitId = null,
    string? ReanalysisBaseCommitId = null) : IRequest<PullRequestAnalysisReportModel>;

/// <summary>
/// Handler for ProcessAnalysisJobCommand following CleanArchitecture pattern.
/// </summary>
public sealed class ProcessAnalysisJobCommandHandler(
    IApplicationDbContext context,
    PullRequestService prService,
    IAnalyzerService analyzerService,
    IWorkItemService workItemService,
    IOptions<AnalysisOptions> analysisOptions,
    ILogger<ProcessAnalysisJobCommandHandler> logger) : IRequestHandler<ProcessAnalysisJobCommand, PullRequestAnalysisReportModel>
{
    private readonly AnalysisOptions _analysisOptions = analysisOptions.Value;

    public async ValueTask<PullRequestAnalysisReportModel> Handle(ProcessAnalysisJobCommand request, CancellationToken cancellationToken)
    {
        var analysisRequest = request.AnalysisRequest;

        // Step 1: Get and filter diffs and findings

        var (diffFull, diffMode) = await GetAnalysisDiffsAsync(request);


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

        // Step 2b: Resolve linked work items (graceful degradation on failure)
        var workItems = await ResolveWorkItemsAsync(analysisRequest, cancellationToken);

        var aiAnalyzerModel = new AnalyzerServiceModel(
            analysisRequest,
            customInstructions ?? string.Empty,
            WorkItemContext: WorkItemPromptFormatter.ToPromptBlock(workItems),
            WorkItemGoal: WorkItemPromptFormatter.ToGoalPromptLine(workItems));

        // Step 3: Execute analysis tasks in parallel
        var analysisResults = await ExecuteAnalysisTasksAsync(analyzerService, aiAnalyzerModel, diffFull, analysisRequest, cancellationToken);

        // Step 4: Post results to PR
        var contextFooter = BuildContextFooter(workItems, customInstructions, diffMode);
        await PostResultsToPullRequestAsync(prService, request.JobId, analysisRequest, analysisResults, contextFooter, cancellationToken);

        // Step 5: Return report
        return BuildAnalysisReport(analysisRequest, analysisResults, diffFull);
    }

    /// <summary>
    /// Fetches the diffs to analyze: for incremental re-analysis jobs the compact diff between the
    /// previously analyzed commit and the current source head, otherwise the full PR diff.
    /// Falls back to the full PR diff when the incremental diff fails (e.g. after a force-push).
    /// </summary>
    private async Task<(Dictionary<string, string> Diffs, string DiffMode)> GetAnalysisDiffsAsync(ProcessAnalysisJobCommand request)
    {
        var analysisRequest = request.AnalysisRequest;
        if (request.ReanalysisBaseCommitId is null || request.SourceCommitId is null)
        {
            return (await prService.GetCompactDiffsAsync(analysisRequest, _analysisOptions.ContextLines), "full");
        }

        try
        {
            var diffs = await prService.GetCompactDiffsBetweenCommitsAsync(
                analysisRequest,
                request.ReanalysisBaseCommitId,
                request.SourceCommitId,
                _analysisOptions.ContextLines);
            return (diffs, "incremental");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Incremental diff {BaseCommitId}..{SourceCommitId} failed for PR #{PullRequestId}; falling back to full PR diff",
                request.ReanalysisBaseCommitId,
                request.SourceCommitId,
                analysisRequest.GitInfo?.PullRequestId);
            return (await prService.GetCompactDiffsAsync(analysisRequest, _analysisOptions.ContextLines), "full (incremental fallback)");
        }
    }

    private async Task<AnalysisResults> ExecuteAnalysisTasksAsync(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        // The batched run always generates summary, detailed analysis and inline suggestions in one
        // pass; for inline-only jobs (incremental re-analysis) the individual-task path below makes
        // a single inline call instead of paying for outputs that would never be posted.
        // SynchronousAnalysis forces the individual-task path: the batch tier has no latency
        // guarantee, so direct parallel calls are used when review turnaround matters more than cost.
        var needsSummaryOrDetailed = analysisRequest.EnableDescriptionSummary || analysisRequest.EnableSummaryComment;

        if (!_analysisOptions.SynchronousAnalysis && analyzer is IBatchAnalyzerService batchAnalyzer && needsSummaryOrDetailed)
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
        var diffPartial = needsSummaryOrDetailed
            ? await prService.GetCompactDiffsAsync(analysisRequest, contextLines: _analysisOptions.DetailedContextLines)
            : [];

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
            logger.LogInformation(
                "Code owners enabled but no CODEOWNERS file found in repository {RepositoryName}",
                analysisRequest.GitInfo?.RepositoryName);
            return null;
        }

        var filtered = CodeOwnersPathFilter.FilterMatchingLines(codeOwnersContent, changedFilePaths);
        if (string.IsNullOrEmpty(filtered))
        {
            logger.LogInformation("Code owners enabled but no CODEOWNERS rules match the changed files");
            return null;
        }

        return filtered;
    }

    /// <summary>
    /// Posts the analysis results to the pull request. Each step is isolated: a PR that became
    /// read-only mid-analysis (e.g. completed/abandoned, TF401181) must not fail the job or
    /// block the remaining steps — the analysis is already done and stored.
    /// </summary>
    private async Task PostResultsToPullRequestAsync(
        PullRequestService prService,
        Guid jobId,
        AnalysisRequest analysisRequest,
        AnalysisResults results,
        string contextFooter,
        CancellationToken cancellationToken)
    {
        var failedSteps = new List<string>();

        if (!string.IsNullOrWhiteSpace(results.DetailedAnalysis) && analysisRequest.EnableSummaryComment)
        {
            var initialCommentThreadId = await GetInitialCommentThreadIdAsync(jobId, cancellationToken);
            var commentBody = $"{results.DetailedAnalysis}\n\n---\n<sub>{contextFooter}</sub>";

            await TryPostStepAsync("summary comment", failedSteps, analysisRequest,
                () => prService.AddCommentAsync(analysisRequest, commentBody, threadId: initialCommentThreadId, isResolved: true));
        }

        if (!string.IsNullOrWhiteSpace(results.Summary) && analysisRequest.EnableDescriptionSummary)
        {
            await TryPostStepAsync("description summary", failedSteps, analysisRequest,
                () => prService.AppendToDescriptionAsync(analysisRequest, results.Summary));
        }

        if (analysisRequest.EnableInlineSuggestions && results.InlineSuggestions.Count > 0)
        {
            await TryPostStepAsync("inline suggestions", failedSteps, analysisRequest,
                () => PostInlineSuggestionsAsync(prService, analysisRequest, results.InlineSuggestions));
        }

        var codeOwners = results.CodeOwners;
        if (analysisRequest.EnableAzureDevopsCodeOwners && codeOwners?.CodeOwners.Count > 0)
        {
            await TryPostStepAsync("code owners", failedSteps, analysisRequest,
                () => prService.AddCodeOwnersToPullRequest(analysisRequest, codeOwners));
        }
        else if (analysisRequest.EnableAzureDevopsCodeOwners)
        {
            logger.LogInformation("Code owners enabled but analysis produced no code owners to assign");
        }

        if (failedSteps.Count > 0)
        {
            logger.LogWarning(
                "Analysis for PR #{PullRequestId} completed but {FailedCount} posting step(s) failed: {FailedSteps}",
                analysisRequest.GitInfo?.PullRequestId, failedSteps.Count, string.Join(", ", failedSteps));
        }
    }

    private async Task TryPostStepAsync(string step, List<string> failedSteps, AnalysisRequest analysisRequest, Func<Task> postAction)
    {
        try
        {
            await postAction();
        }
        catch (Exception ex)
        {
            failedSteps.Add(step);
            logger.LogError(ex,
                "Posting step '{Step}' failed for PR #{PullRequestId}; continuing with the remaining steps",
                step, analysisRequest.GitInfo?.PullRequestId);
        }
    }

    /// <summary>
    /// One-line footer appended to the review comment so silently-degrading context enrichments
    /// (work items, custom instructions, incremental diff fallback) are visible on the PR itself.
    /// </summary>
    internal static string BuildContextFooter(List<WorkItemReference> workItems, string? customInstructions, string diffMode)
    {
        var workItemPart = workItems.Count > 0
            ? $"work items: {string.Join(", ", workItems.Select(item => $"#{item.Id}"))}"
            : "work items: none";
        var instructionsPart = string.IsNullOrWhiteSpace(customInstructions)
            ? "custom instructions: none"
            : "custom instructions: found";

        return $"Lintellect context — {workItemPart} · {instructionsPart} · diff: {diffMode}";
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

    private async Task<List<WorkItemReference>> ResolveWorkItemsAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken)
    {
        if (!analysisRequest.EnableWorkItemContext)
        {
            return [];
        }

        try
        {
            var items = await workItemService.ResolveAsync(analysisRequest, cancellationToken);
            if (items.Count == 0)
            {
                logger.LogInformation("Work item context enabled but no linked items found for PR #{PullRequestId}",
                    analysisRequest.GitInfo?.PullRequestId);
            }

            return items;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Work item context resolution failed for PR #{PullRequestId}; continuing without context",
                analysisRequest.GitInfo?.PullRequestId);
            return [];
        }
    }

    private async Task<int?> GetInitialCommentThreadIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await context.AnalysisJobs
            .Where(job => job.Id == jobId)
            .Select(job => job.InitialCommentThreadId)
            .FirstOrDefaultAsync(cancellationToken);
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
