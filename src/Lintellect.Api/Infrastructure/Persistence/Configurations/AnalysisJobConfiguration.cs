using Lintellect.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

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


        // Configure JsonDocument for PostgreSQL JSONB storage
        builder.Property(e => e.AnalysisRequest)
            .HasColumnType("jsonb");

        // Configure indexes
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Created);
    }
}
