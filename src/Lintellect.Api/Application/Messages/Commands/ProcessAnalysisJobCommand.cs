using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Extensions;
using Lintellect.Shared.Models;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands;

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
    PullRequestService prService,
    IAnalyzerServiceResolver analyzerResolver) : IRequestHandler<ProcessAnalysisJobCommand, PullRequestAnalysisReportModel>
{
    public async ValueTask<PullRequestAnalysisReportModel> Handle(ProcessAnalysisJobCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Get and filter diffs and findings
        var analysisRequest = request.AnalysisRequest;

        var diffs = await GetFilteredDiffsAsync(analysisRequest, cancellationToken);

        // Apply file exclusions if specified
        if (analysisRequest.FileExclusions != null && analysisRequest.FileExclusions.Count > 0)
        {
            var filteredFiles = FilePatternMatcher.FilterFiles(diffs.Keys, analysisRequest.FileExclusions);
            diffs = filteredFiles.ToDictionary(file => file, file => diffs[file]);
        }

        // Filter findings to only include those for files that exist in diffs
        analysisRequest.Findings = [.. analysisRequest.Findings.Where(finding =>
            diffs.ContainsKey(finding.FilePath))];


        // Step 2: Prepare analyzer and custom instructions
        var analyzer = analyzerResolver.GetAnalyzerService(EAnalyzers.AIFoundry);
        var customInstructions = await prService.GetCustomInstructionsAsync(analysisRequest);
        var aiAnalyzerModel = new AnalyzerServiceModel(analysisRequest, customInstructions ?? string.Empty);

        // Step 3: Execute analysis tasks in parallel
        var analysisResults = await ExecuteAnalysisTasksAsync(analyzer, aiAnalyzerModel, diffs, analysisRequest, cancellationToken);

        // Step 4: Post results to PR
        await PostResultsToPullRequestAsync(prService, analysisRequest, analysisResults, cancellationToken);

        // Step 5: Return report
        return BuildAnalysisReport(analysisRequest, analysisResults, diffs);
    }

    private async Task<Dictionary<string, string>> GetFilteredDiffsAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken)
    {
        // Get diffs from Git provider
        var diffs = await prService.GetCompactDiffsAsync(
            analysisRequest,
            contextLines: 3,
            maxNewFileLines: 50,
            maxLinesPerFile: 1000);

        return diffs;
    }

    private async Task<AnalysisResults> ExecuteAnalysisTasksAsync(
        IAnalyzerService analyzer,
        AnalyzerServiceModel aiAnalyzerModel,
        Dictionary<string, string> diffs,
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var summaryTask = CreateSummaryTaskIfEnabled(analyzer, aiAnalyzerModel, diffs, analysisRequest, cancellationToken);
        var detailedAnalysisTask = CreateDetailedAnalysisTaskIfEnabled(analyzer, aiAnalyzerModel, diffs, analysisRequest, cancellationToken);
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
            ? analyzer.AnalyzeAsync(aiAnalyzerModel, diffs, cancellationToken)
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

        var codeOwnersContent = await prService.GetCodeOwnersFileAsync(analysisRequest);

        return codeOwnersContent == null ? null : await analyzer.GetCodeOwnersAsync(codeOwnersContent, changedFilePaths, cancellationToken);
    }

    private async Task PostResultsToPullRequestAsync(
        PullRequestService prService,
        AnalysisRequest analysisRequest,
        AnalysisResults results,
        CancellationToken cancellationToken)
    {
        // Post detailed analysis comment
        if (!string.IsNullOrWhiteSpace(results.DetailedAnalysis) && analysisRequest.EnableSummaryComment)
        {
            await prService.AddCommentAsync(analysisRequest, results.DetailedAnalysis);
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

    private async Task PostInlineSuggestionsAsync(
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
            AnalyzerUsed = EAnalyzers.AIFoundry.ToString(),
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
