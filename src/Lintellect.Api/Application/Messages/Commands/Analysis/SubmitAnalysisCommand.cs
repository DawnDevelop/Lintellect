using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Models;
using Mediator;

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
        var analysisJob = new AnalysisJob(request.AnalysisRequest);

        if (request.AnalysisRequest.EnableInitialComment && request.AnalysisRequest.EnableSummaryComment)
        {
            await TryPostInitialCommentAsync(analysisJob, request.AnalysisRequest);
        }

        context.AnalysisJobs.Add(analysisJob);
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(analysisJob, cancellationToken);

        return analysisJob.Id;
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
}
