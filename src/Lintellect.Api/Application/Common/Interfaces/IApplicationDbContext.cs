using Lintellect.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Common.Interfaces;

/// <summary>
/// Application DbContext interface following CleanArchitecture pattern.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<AnalysisJob> AnalysisJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
