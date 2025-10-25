using devops_pr_analyzer.Application.Common.Interfaces;
using devops_pr_analyzer.shared.Models;
using MediatR;

namespace devops_pr_analyzer.Application.Messages.Commands;

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
public sealed class CompleteAnalysisJobCommandHandler(IApplicationDbContext context) : IRequestHandler<CompleteAnalysisJobCommand>
{
    public async Task Handle(CompleteAnalysisJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.AnalysisJobs.FindAsync(request.JobId, cancellationToken);
        if (job == null)
            return;

        job.Complete(
            request.Summary,
            request.DetailedAnalysis,
            request.InlineSuggestions,
            request.AnalyzerUsed);

        await context.SaveChangesAsync(cancellationToken);
    }
}
