using Lintellect.Api.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

/// <summary>
/// Command to complete analysis job with results
/// </summary>
public sealed record CompleteAnalysisJobCommand(
    Guid JobId,
    string Summary,
    string DetailedAnalysis,
    string? InlineSuggestions = null,
    string AnalyzerUsed = "Unknown") : IRequest;

/// <summary>
/// Handler for CompleteAnalysisJobCommand following CleanArchitecture pattern.
/// </summary>
public sealed class CompleteAnalysisJobCommandHandler(IApplicationDbContext context) : IRequestHandler<CompleteAnalysisJobCommand, Unit>
{
    public async ValueTask<Unit> Handle(CompleteAnalysisJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.AnalysisJobs.FirstOrDefaultAsync(x => x.Id == request.JobId, cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException($"Analysis job with ID {request.JobId} was not found.");
        }

        job.Complete(
            request.Summary,
            request.DetailedAnalysis,
            request.InlineSuggestions,
            request.AnalyzerUsed);

        await context.SaveChangesAsync(cancellationToken);

        return default;
    }

}
