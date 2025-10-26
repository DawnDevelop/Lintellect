using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Domain.Events;
using Lintellect.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for the application following CleanArchitecture pattern.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore domain events - they are not database entities
        modelBuilder.Ignore<Domain.Events.BaseEvent>();
        modelBuilder.Ignore<Domain.Events.AnalysisJobCreatedEvent>();
        modelBuilder.Ignore<Domain.Events.AnalysisJobStartedEvent>();
        modelBuilder.Ignore<AnalysisJobCompletedEvent>();
        modelBuilder.Ignore<AnalysisJobFailedEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
