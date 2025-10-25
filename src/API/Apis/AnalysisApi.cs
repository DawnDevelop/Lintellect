using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.Apis.Models;
using devops_pr_analyzer.Application.Messages.Commands;
using devops_pr_analyzer.Application.Messages.Queries;
using devops_pr_analyzer.Infrastructure.Services;
using devops_pr_analyzer.Infrastructure.Services.Git;
using devops_pr_analyzer.shared.Models;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace devops_pr_analyzer.Apis;

public static class AnalysisApi
{
    public static IEndpointRouteBuilder MapAnalysisApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/analysis")
            .WithTags("Analysis");
        //.AddEndpointFilter<ApiKeyEndpointFilter>();

        api.MapPost("/analyze", SubmitAnalysis)
            .WithName("SubmitAnalysis")
            .WithSummary("Submit analysis job")
            .WithDescription("Submit a new analysis job for background processing.");

        api.MapGet("/status/{jobId:guid}", GetAnalysisStatus)
            .WithName("GetAnalysisStatus")
            .WithSummary("Get analysis job status")
            .WithDescription("Get the status and results of an analysis job.");

        api.MapGet("/history", GetAnalysisHistory)
            .WithName("GetAnalysisHistory")
            .WithSummary("Get analysis history")
            .WithDescription("Get the history of analysis jobs with optional filtering.");

        return app;
    }

    private static async Task<Accepted<SubmitAnalysisResponse>> SubmitAnalysis(
        [FromServices] IMediator mediator,
        [FromServices] AnalysisJobQueue jobQueue,
        [FromBody] SubmitAnalysisCommand command,
        CancellationToken cancellationToken)
    {

        var jobId = await mediator.Send(command, cancellationToken);

        return TypedResults.Accepted($"/api/analysis/status/{jobId}", new SubmitAnalysisResponse(jobId,
            "Pending",
            "Analysis job submitted successfully"));
    }

    private static async Task<Results<Ok<AnalysisJobStatusResponse>, NotFound>> GetAnalysisStatus(
        [FromServices] IMediator mediator,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new GetAnalysisStatusQuery(jobId), cancellationToken);

        if (job == null)
        {
            return TypedResults.NotFound();
        }

        // Get CLI analysis result from JsonDocument
        var analysisResult = job.AnalysisRequest?.Deserialize<AnalysisRequest>();

        return TypedResults.Ok(new AnalysisJobStatusResponse(
            job.Id,
            job.Status.ToString(),
            analysisResult?.GitInfo?.ProjectName ?? "Unknown",
            analysisResult?.GitInfo?.RepositoryName ?? "Unknown",
            analysisResult?.GitInfo?.PullRequestId ?? 0,
            job.Created,
            job.StartedAt,
            job.CompletedAt,
            job.ErrorMessage,
            analysisResult,
            job.Summary,
            job.DetailedAnalysis,
            job.InlineSuggestions,
            job.AnalyzerUsed
        ));
    }

    private static async Task<Ok<IEnumerable<AnalysisJobStatusResponse>>> GetAnalysisHistory(
        [FromServices] IMediator mediator,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? projectName = null,
        [FromQuery] string? repositoryName = null,
        CancellationToken cancellationToken = default)
    {
        var jobs = await mediator.Send(new GetAnalysisHistoryQuery(skip, take, projectName, repositoryName), cancellationToken);

        var response = jobs.Select(job =>
        {
            // Get CLI analysis result from JsonDocument
            var analysisResult = job.AnalysisRequest?.Deserialize<AnalysisRequest>(); ;

            return new AnalysisJobStatusResponse(
                job.Id,
                job.Status.ToString(),
                analysisResult?.GitInfo?.ProjectName ?? "Unknown",
                analysisResult?.GitInfo?.RepositoryName ?? "Unknown",
                analysisResult?.GitInfo?.PullRequestId ?? 0,
                job.Created,
                job.StartedAt,
                job.CompletedAt,
                job.ErrorMessage,
                analysisResult,
                job.Summary,
                job.DetailedAnalysis,
                job.InlineSuggestions,
                job.AnalyzerUsed
            );
        });

        return TypedResults.Ok(response);
    }
}
