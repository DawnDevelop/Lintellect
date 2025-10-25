using devops_pr_analyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace devops_pr_analyzer.Application.Common.Interfaces;

/// <summary>
/// Application DbContext interface following CleanArchitecture pattern.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<AnalysisJob> AnalysisJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
