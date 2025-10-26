using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Shared.Models;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands;

/// <summary>
/// Command to complete analysis job with results following CleanArchitecture pattern.
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
        var job = await context.AnalysisJobs.FindAsync(request.JobId, cancellationToken);
        if(job is not null)
        {
            job.Complete(
                request.Summary,
                request.DetailedAnalysis,
                request.InlineSuggestions,
                request.AnalyzerUsed);

            await context.SaveChangesAsync(cancellationToken);
        }
        

        return default;
    }

}
