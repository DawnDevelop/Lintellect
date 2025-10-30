using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Enums;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

/// <summary>
/// Command to update analysis job status following CleanArchitecture pattern.
/// </summary>
public sealed record UpdateAnalysisJobStatusCommand(
    Guid JobId,
    AnalysisStatus Status,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null) : IRequest;

/// <summary>
/// Handler for UpdateAnalysisJobStatusCommand following CleanArchitecture pattern.
/// </summary>
public sealed class UpdateAnalysisJobStatusCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateAnalysisJobStatusCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateAnalysisJobStatusCommand request, CancellationToken cancellationToken)
    {
        var job = await context.AnalysisJobs.FindAsync([request.JobId], cancellationToken: cancellationToken);
        if (job is null)
        {
            return default;
        }

        if (request.Status == AnalysisStatus.Running && job.Status == AnalysisStatus.Pending)
        {
            job.Start();
        }
        else if (request.Status == AnalysisStatus.Failed && !string.IsNullOrEmpty(request.ErrorMessage))
        {
            job.Fail(request.ErrorMessage);
        }

        await context.SaveChangesAsync(cancellationToken);
        return default;
    }
}
