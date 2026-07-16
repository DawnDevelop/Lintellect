using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

/// <summary>
/// Command to submit a new analysis job following CleanArchitecture pattern.
/// </summary>
public sealed record SubmitAnalysisCommand(
    AnalysisRequest AnalysisRequest) : IRequest<Guid>;

/// <summary>
/// Handler for SubmitAnalysisCommand following CleanArchitecture pattern.
/// </summary>
public sealed class SubmitAnalysisCommandHandler(
    IApplicationDbContext context,
    AnalysisJobQueue queue,
    PullRequestService prService,
    ILogger<SubmitAnalysisCommandHandler> logger) : IRequestHandler<SubmitAnalysisCommand, Guid>
{
    private const string InitialCommentText =
        "🔄 Lintellect analysis is in progress. This comment will update automatically once the review is ready.";

    public async ValueTask<Guid> Handle(SubmitAnalysisCommand request, CancellationToken cancellationToken)
    {
        var analysisRequest = request.AnalysisRequest;
        var previousJob = await FindLatestTriggeredJobAsync(analysisRequest, cancellationToken);
        var sourceCommitId = await TryResolveSourceCommitIdAsync(analysisRequest);

        if (previousJob is not null)
        {
            var skipReason = GetSkipReason(previousJob, analysisRequest, sourceCommitId);
            if (skipReason is not null)
            {
                logger.LogInformation(
                    "Skipping analysis for PR #{PullRequestId} in {RepositoryName}: {SkipReason}; returning job {JobId}",
                    analysisRequest.GitInfo?.PullRequestId,
                    analysisRequest.GitInfo?.RepositoryName,
                    skipReason,
                    previousJob.Id);
                return previousJob.Id;
            }

            ConfigureForReanalysis(analysisRequest);
            logger.LogInformation(
                "PR #{PullRequestId} in {RepositoryName} was already analyzed; queuing inline-only re-analysis from {BaseCommitId}",
                analysisRequest.GitInfo?.PullRequestId,
                analysisRequest.GitInfo?.RepositoryName,
                previousJob.SourceCommitId);
        }

        var analysisJob = new AnalysisJob(analysisRequest, sourceCommitId, previousJob?.SourceCommitId);

        if (analysisRequest.EnableInitialComment && analysisRequest.EnableSummaryComment)
        {
            await TryPostInitialCommentAsync(analysisJob, analysisRequest);
        }

        context.AnalysisJobs.Add(analysisJob);
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(analysisJob, cancellationToken);

        return analysisJob.Id;
    }

    private async Task<TriggeredJob?> FindLatestTriggeredJobAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken)
    {
        var gitInfo = analysisRequest.GitInfo;
        if (gitInfo is null)
        {
            return null;
        }

        return await context.AnalysisJobs
            .Where(job =>
                job.Status != AnalysisStatus.Failed &&
                job.AnalysisRequest!.GitProvider == analysisRequest.GitProvider &&
                job.AnalysisRequest.GitInfo!.PullRequestId == gitInfo.PullRequestId &&
                job.AnalysisRequest.GitInfo.RepositoryName == gitInfo.RepositoryName &&
                job.AnalysisRequest.GitInfo.ProjectName == gitInfo.ProjectName)
            .OrderByDescending(job => job.Created)
            .Select(job => new TriggeredJob(job.Id, job.Status, job.SourceCommitId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> TryResolveSourceCommitIdAsync(AnalysisRequest analysisRequest)
    {
        if (analysisRequest.GitInfo is null)
        {
            return null;
        }

        try
        {
            var pullRequest = await prService.GetPullRequestAsync(analysisRequest);
            return pullRequest.SourceCommit?.CommitId;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to resolve source head commit for PR #{PullRequestId}; continuing without it",
                analysisRequest.GitInfo.PullRequestId);
            return null;
        }
    }

    private static string? GetSkipReason(TriggeredJob previousJob, AnalysisRequest analysisRequest, string? sourceCommitId)
    {
        if (previousJob.Status != AnalysisStatus.Completed)
        {
            return $"previous job is still {previousJob.Status}";
        }

        if (sourceCommitId is not null && sourceCommitId == previousJob.SourceCommitId)
        {
            return "source branch has no new commits since the last analysis";
        }

        if (!analysisRequest.EnableInlineSuggestions)
        {
            return "inline suggestions are disabled, so a re-analysis would produce nothing";
        }

        return null;
    }

    private static void ConfigureForReanalysis(AnalysisRequest analysisRequest)
    {
        analysisRequest.EnableInitialComment = false;
        analysisRequest.EnableSummaryComment = false;
        analysisRequest.EnableDescriptionSummary = false;
        analysisRequest.EnableAzureDevopsCodeOwners = false;
    }

    private async Task TryPostInitialCommentAsync(AnalysisJob analysisJob, AnalysisRequest analysisRequest)
    {
        try
        {
            var placeholder = await prService.AddCommentAsync(analysisRequest, InitialCommentText);
            analysisJob.SetInitialCommentThreadId(placeholder.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to post initial placeholder comment for PR #{PullRequestId}; continuing without it",
                analysisRequest.GitInfo?.PullRequestId);
        }
    }

    private sealed record TriggeredJob(Guid Id, AnalysisStatus Status, string? SourceCommitId);
}
