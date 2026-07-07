using Lintellect.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lintellect.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AnalysisJob entity.
/// </summary>
public sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.HasKey(e => e.Id);


        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.StartedAt);

        builder.Property(e => e.CompletedAt);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.Summary);

        builder.Property(e => e.DetailedAnalysis);

        builder.Property(e => e.InlineSuggestions);

        builder.Property(e => e.AnalyzerUsed)
            .HasMaxLength(100);

        builder.Property(e => e.InitialCommentThreadId);

        builder.OwnsOne(j => j.AnalysisRequest, ar =>
        {
            ar.ToJson(); // Maps to a JSON column
            ar.OwnsOne(a => a.GitInfo);
            // Findings is part of the JSON, ignore it as a navigation property
            ar.Ignore(a => a.Findings);
            // WorkItems are CLI-provided hints, in-memory only during processing
            ar.Ignore(a => a.WorkItems);
        });

        // Configure indexes
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Created);
    }

}
