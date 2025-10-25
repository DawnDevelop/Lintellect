using devops_pr_analyzer.Application.Common.Interfaces;
using devops_pr_analyzer.Domain.Entities;
using devops_pr_analyzer.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace devops_pr_analyzer.Infrastructure.Persistence;

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
        modelBuilder.Ignore<Domain.Events.AnalysisJobCompletedEvent>();
        modelBuilder.Ignore<Domain.Events.AnalysisJobFailedEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
