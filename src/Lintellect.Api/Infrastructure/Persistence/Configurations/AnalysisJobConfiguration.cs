using System.Text.Json;
using Lintellect.Api.Domain.Entities;
using Lintellect.Shared.Models;
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


        // Configure JsonDocument for PostgreSQL JSONB storage with sanitization
        builder.Property(e => e.AnalysisRequest)
            .HasColumnType("jsonb")
            .HasConversion(
                // Convert to sanitized JSON for storage
                v => v == null ? null : SanitizeAnalysisRequest(v),
                // Convert from JSON when reading
                v => v == null ? null : JsonDocument.Parse(v, new JsonDocumentOptions()));

        // Configure indexes
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Created);
    }

    /// <summary>
    /// Sanitizes an AnalysisRequest JsonDocument by removing sensitive information
    /// </summary>
    /// <param name="analysisRequest">The original analysis request</param>
    /// <returns>A JSON string with sensitive data removed</returns>
    private static string SanitizeAnalysisRequest(JsonDocument analysisRequest)
    {
        if (analysisRequest == null)
        {
            return "{}";
        }

        try
        {
            // Deserialize to AnalysisRequest
            var originalRequest = JsonSerializer.Deserialize<AnalysisRequest>(analysisRequest.RootElement.GetRawText());
            if (originalRequest == null)
            {
                return "{}";
            }

            // Create sanitized version
            var sanitizedRequest = SanitizedAnalysisRequest.FromAnalysisRequest(originalRequest);

            // Return sanitized JSON
            return JsonSerializer.Serialize(sanitizedRequest);
        }
        catch (JsonException)
        {
            // If deserialization fails, return empty object
            return "{}";
        }
    }
}
