using devops_pr_analyzer.Application.Common.Interfaces;
using devops_pr_analyzer.Domain.Entities;
using devops_pr_analyzer.Domain.Enums;
using devops_pr_analyzer.Infrastructure.Services;
using devops_pr_analyzer.shared.Models;
using MediatR;

namespace devops_pr_analyzer.Application.Messages.Commands;

/// <summary>
/// Command to submit a new analysis job following CleanArchitecture pattern.
/// </summary>
public sealed record SubmitAnalysisCommand(
    AnalysisRequest AnalysisRequest) : IRequest<Guid>;

/// <summary>
/// Handler for SubmitAnalysisCommand following CleanArchitecture pattern.
/// </summary>
public sealed class SubmitAnalysisCommandHandler(IApplicationDbContext context, AnalysisJobQueue queue) : IRequestHandler<SubmitAnalysisCommand, Guid>
{
    public async Task<Guid> Handle(SubmitAnalysisCommand request, CancellationToken cancellationToken)
    {
        var analysisJob = new AnalysisJob(request.AnalysisRequest);

        context.AnalysisJobs.Add(analysisJob);
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(analysisJob, cancellationToken);

        return analysisJob.Id;
    }
}
