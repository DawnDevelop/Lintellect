using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Queries;

/// <summary>
/// Query to get analysis history following CleanArchitecture pattern.
/// </summary>
public sealed record GetAnalysisHistoryQuery(
    int Skip = 0,
    int Take = 50,
    string? ProjectName = null,
    string? RepositoryName = null) : IRequest<IEnumerable<AnalysisJob>>;

/// <summary>
/// Handler for GetAnalysisHistoryQuery following CleanArchitecture pattern.
/// </summary>
public sealed class GetAnalysisHistoryQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAnalysisHistoryQuery, IEnumerable<AnalysisJob>>
{
    public async ValueTask<IEnumerable<AnalysisJob>> Handle(GetAnalysisHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = context.AnalysisJobs.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProjectName))
        {
            query = query.Where(job => job.AnalysisRequest!.GitInfo!.ProjectName == request.ProjectName);
        }

        if (!string.IsNullOrEmpty(request.RepositoryName))
        {
            query = query.Where(job => job.AnalysisRequest!.GitInfo!.RepositoryName == request.RepositoryName);
        }

        return await query
            .OrderByDescending(job => job.Created)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);
    }
}
