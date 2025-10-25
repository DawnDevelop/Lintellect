using devops_pr_analyzer.Domain.Common;
using devops_pr_analyzer.Domain.Enums;
using devops_pr_analyzer.Domain.Events;
using devops_pr_analyzer.shared.Models;
using System.Text.Json;

namespace devops_pr_analyzer.Domain.Entities;

/// <summary>
/// Represents an analysis job in the system.
/// </summary>
public sealed class AnalysisJob : BaseAuditableEntity
{
    public AnalysisStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Summary { get; private set; }
    public string? DetailedAnalysis { get; private set; }
    public string? InlineSuggestions { get; private set; }
    public string? AnalyzerUsed { get; private set; }
    public JsonDocument? AnalysisRequest { get; private set; } // PostgreSQL JSONB storage

    // Parameterless constructor for EF Core
    private AnalysisJob() { }

    public AnalysisJob(AnalysisRequest cliAnalysisResult)
    {
        Status = AnalysisStatus.Pending;
        AnalysisRequest = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cliAnalysisResult));

        AddDomainEvent(new AnalysisJobCreatedEvent(Id,
            cliAnalysisResult.GitInfo?.ProjectName ?? "Unknown",
            cliAnalysisResult.GitInfo?.RepositoryName ?? "Unknown",
            cliAnalysisResult.GitInfo?.PullRequestId ?? 0));
    }

    public void Start()
    {
        if (Status != AnalysisStatus.Pending)
            throw new InvalidOperationException($"Cannot start job in {Status} status");

        Status = AnalysisStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AnalysisJobStartedEvent(Id));
    }

    public void Complete(string summary, string detailedAnalysis, string? inlineSuggestions, string analyzerUsed)
    {
        if (Status != AnalysisStatus.Running)
            throw new InvalidOperationException($"Cannot complete job in {Status} status");

        Status = AnalysisStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        Summary = summary;
        DetailedAnalysis = detailedAnalysis;
        InlineSuggestions = inlineSuggestions;
        AnalyzerUsed = analyzerUsed;

        AddDomainEvent(new AnalysisJobCompletedEvent(Id, analyzerUsed));
    }

    public void Fail(string errorMessage)
    {
        if (Status != AnalysisStatus.Running && Status != AnalysisStatus.Pending)
            throw new InvalidOperationException($"Cannot fail job in {Status} status");

        Status = AnalysisStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;

        AddDomainEvent(new AnalysisJobFailedEvent(Id, errorMessage));
    }
}
