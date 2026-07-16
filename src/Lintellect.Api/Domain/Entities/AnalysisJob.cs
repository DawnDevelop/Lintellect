using Lintellect.Api.Domain.Common;
using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Domain.Events;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Domain.Entities;

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
    public int? InitialCommentThreadId { get; private set; }

    /// <summary>
    /// The source branch head commit this analysis covers. Used as the diff baseline
    /// for subsequent incremental re-analysis runs on the same pull request.
    /// </summary>
    public string? SourceCommitId { get; private set; }

    /// <summary>
    /// The source branch commit covered by the previous analysis run. When set, this job
    /// is an incremental re-analysis that only reviews changes since that commit.
    /// </summary>
    public string? ReanalysisBaseCommitId { get; private set; }

    public AnalysisRequest? AnalysisRequest { get; private set; }

    // Parameterless constructor for EF Core
    private AnalysisJob() { }

    public AnalysisJob(AnalysisRequest cliAnalysisResult, string? sourceCommitId = null, string? reanalysisBaseCommitId = null)
    {
        ArgumentNullException.ThrowIfNull(cliAnalysisResult);

        Status = AnalysisStatus.Pending;
        AnalysisRequest = cliAnalysisResult;
        SourceCommitId = sourceCommitId;
        ReanalysisBaseCommitId = reanalysisBaseCommitId;

        AddDomainEvent(new AnalysisJobCreatedEvent(Id,
            cliAnalysisResult.GitInfo?.ProjectName ?? "Unknown",
            cliAnalysisResult.GitInfo?.RepositoryName ?? "Unknown",
            cliAnalysisResult.GitInfo?.PullRequestId ?? 0));
    }

    public AnalysisRequest CreateAnalysisRequestSnapshot()
    {
        if (AnalysisRequest is null)
        {
            throw new InvalidOperationException("Analysis request is not available for this job.");
        }

        return CloneAnalysisRequest(AnalysisRequest);
    }

    public void SetInitialCommentThreadId(int threadId)
    {
        InitialCommentThreadId = threadId;
    }

    public void Start()
    {
        if (Status != AnalysisStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start job in {Status} status");
        }

        Status = AnalysisStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AnalysisJobStartedEvent(Id));
    }

    public void Complete(string summary, string detailedAnalysis, string? inlineSuggestions, string analyzerUsed)
    {
        if (Status != AnalysisStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete job in {Status} status");
        }

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
        if (Status is not AnalysisStatus.Running and not AnalysisStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail job in {Status} status");
        }

        Status = AnalysisStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;

        AddDomainEvent(new AnalysisJobFailedEvent(Id, errorMessage));
    }

    private static AnalysisRequest CloneAnalysisRequest(AnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AnalysisRequest
        {
            Language = request.Language,
            Findings = request.Findings?
                .Select(finding => new AnalyzerFindings
                {
                    RuleId = finding.RuleId,
                    Message = finding.Message,
                    FilePath = finding.FilePath,
                    Line = finding.Line,
                    Severity = finding.Severity
                })
                .ToList() ?? new List<AnalyzerFindings>(),
            GitInfo = request.GitInfo is null
                ? null
                : new GitInfo(
                    request.GitInfo.PullRequestId,
                    request.GitInfo.CommitId,
                    request.GitInfo.RepositoryName,
                    request.GitInfo.Type,
                    request.GitInfo.ProjectName),
            GitProvider = request.GitProvider,
            FileExclusions = request.FileExclusions is null ? [] : [.. request.FileExclusions],
            EnableSummaryComment = request.EnableSummaryComment,
            EnableInitialComment = request.EnableInitialComment,
            EnableInlineSuggestions = request.EnableInlineSuggestions,
            EnableDescriptionSummary = request.EnableDescriptionSummary,
            EnableAzureDevopsCodeOwners = request.EnableAzureDevopsCodeOwners,
            EnableWorkItemContext = request.EnableWorkItemContext,
            WorkItems = request.WorkItems is null ? [] : [.. request.WorkItems],
            McpServer = request.McpServer is null ? [] : [.. request.McpServer],
        };
    }
}
