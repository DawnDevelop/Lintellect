using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.Models;
using devops_pr_analyzer.Services;
using devops_pr_analyzer.Services.Git;
using devops_pr_analyzer.shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace devops_pr_analyzer.Apis;

public static class AnalysisApi
{
    public static IEndpointRouteBuilder MapAnalysisApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/analysis")
            .WithTags("Analysis")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        api.MapPost("/result", PostAnalysisResult)
            .WithName("PostAnalysisResult")
            .WithSummary("Submit analysis result")
            .WithDescription("Submit the result of a static analysis run and get compact diffs for AI analysis.");

        api.MapPost("/analyze", AnalyzePullRequest)
            .WithName("AnalyzePullRequest")
            .WithSummary("Analyze pull request with AI")
            .WithDescription("Perform complete AI-powered analysis of a pull request including diffs and findings.");

        api.MapPost("/analyze/summary", GenerateSummary)
            .WithName("GeneratePRSummary")
            .WithSummary("Generate PR summary")
            .WithDescription("Generate a quick summary of the pull request for fast feedback.");

        return app;
    }

    private static async Task<Ok<Dictionary<string, string>>> PostAnalysisResult(
        [FromServices] PullRequestService diffService,
        AnalysisResult analysisResult)
    {
        // Get compact diffs optimized for token usage
        // The service automatically selects the right Git provider (Azure DevOps, GitHub, etc.)
        var compactDiffs = await diffService.GetCompactDiffsAsync(
            analysisResult,
            contextLines: 3,           // Lines of context around each change
            maxNewFileLines: 50,       // Max lines to show for new/deleted files
            maxLinesPerFile: 1000     // Max total lines per file diff
        );

        return TypedResults.Ok(compactDiffs);
    }

    private static async Task<Ok<PullRequestAnalysisReportModel>> AnalyzePullRequest(
        [FromServices] PullRequestAnalysisOrchestrator orchestrator,
        [FromBody] AnalyzePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var options = AnalysisOptions.Comprehensive;

        var report = await orchestrator.AnalyzeAsync(
            request.AnalysisResult,
            request.Analyzer,
            options,
            cancellationToken);

        return TypedResults.Ok(report);
    }

    private static async Task<Ok<PullRequestSummaryResponse>> GenerateSummary(
        [FromServices] PullRequestAnalysisOrchestrator orchestrator,
        [FromBody] AnalyzePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var options = AnalysisOptions.Default;

        var summary = await orchestrator.GenerateQuickSummaryAsync(
            request.AnalysisResult,
            request.Analyzer,
            options,
            cancellationToken);

        return TypedResults.Ok(new PullRequestSummaryResponse
        {
            Summary = summary,
            Analyzer = request.Analyzer.ToString(),
            GeneratedAt = DateTimeOffset.UtcNow
        });
    }
}

/// <summary>
/// Request to analyze a pull request with AI.
/// </summary>
public sealed record AnalyzePullRequestRequest
{
    /// <summary>
    /// The analysis result from static code analyzers.
    /// </summary>
    public required AnalysisResult AnalysisResult { get; init; }

    /// <summary>
    /// The AI analyzer to use.
    /// </summary>
    public EAnalyzers Analyzer { get; init; } = EAnalyzers.AIFoundry;

    /// <summary>
    /// Use compact mode for token optimization (smaller diffs, only files with findings).
    /// </summary>
    public bool UseCompactMode { get; init; } = false;

    /// <summary>
    /// Use comprehensive mode for detailed analysis (larger diffs, all files).
    /// </summary>
    public bool UseComprehensiveMode { get; init; } = false;

    public bool EnableCodeOwners { get; init; } = false;
}

/// <summary>
/// Response containing a PR summary.
/// </summary>
public sealed record PullRequestSummaryResponse
{
    /// <summary>
    /// The generated summary.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// The analyzer that generated the summary.
    /// </summary>
    public required string Analyzer { get; init; }

    /// <summary>
    /// When the summary was generated.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }
}
