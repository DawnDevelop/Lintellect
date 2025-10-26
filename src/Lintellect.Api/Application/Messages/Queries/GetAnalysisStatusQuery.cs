using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Queries;

/// <summary>
/// Query to get analysis job status following CleanArchitecture pattern.
/// </summary>
public sealed record GetAnalysisStatusQuery(Guid JobId) : IRequest<AnalysisJob?>;

/// <summary>
/// Handler for GetAnalysisStatusQuery following CleanArchitecture pattern.
/// </summary>
public sealed class GetAnalysisStatusQueryHandler : IRequestHandler<GetAnalysisStatusQuery, AnalysisJob?>
{
    private readonly IApplicationDbContext _context;

    public GetAnalysisStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async ValueTask<AnalysisJob?> Handle(GetAnalysisStatusQuery request, CancellationToken cancellationToken)
    {
        return await _context.AnalysisJobs
            .FirstOrDefaultAsync(job => job.Id == request.JobId, cancellationToken);
    }
}
