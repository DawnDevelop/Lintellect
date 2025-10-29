using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Infrastructure.Services.Analysis;
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
public sealed class SubmitAnalysisCommandHandler(IApplicationDbContext context, AnalysisJobQueue queue) : IRequestHandler<SubmitAnalysisCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitAnalysisCommand request, CancellationToken cancellationToken)
    {
        var analysisJob = new AnalysisJob(request.AnalysisRequest);

        context.AnalysisJobs.Add(analysisJob);
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(analysisJob, cancellationToken);

        return analysisJob.Id;
    }
}
