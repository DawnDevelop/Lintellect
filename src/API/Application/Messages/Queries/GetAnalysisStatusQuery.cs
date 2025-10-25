using devops_pr_analyzer.Application.Common.Interfaces;
using devops_pr_analyzer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace devops_pr_analyzer.Application.Messages.Queries;

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

    public async Task<AnalysisJob?> Handle(GetAnalysisStatusQuery request, CancellationToken cancellationToken)
    {
        return await _context.AnalysisJobs
            .FirstOrDefaultAsync(job => job.Id == request.JobId, cancellationToken);
    }
}
