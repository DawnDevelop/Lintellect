namespace Lintellect.Api.Application.Models.Git;

/// <summary>
/// Represents a pull request from any Git provider.
/// This is a provider-agnostic model following Clean Architecture principles.
/// </summary>
public sealed class PullRequest
{
    /// <summary>
    /// The unique identifier of the pull request.
    /// </summary>
    public required int PullRequestId { get; init; }

    /// <summary>
    /// The title of the pull request.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// The description/body of the pull request.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The source branch reference (e.g., "refs/heads/feature-branch").
    /// </summary>
    public string? SourceRefName { get; init; }

    /// <summary>
    /// The target branch reference (e.g., "refs/heads/main").
    /// </summary>
    public string? TargetRefName { get; init; }

    /// <summary>
    /// The status of the pull request (e.g., Active, Completed, Abandoned).
    /// </summary>
    public PullRequestStatus Status { get; init; }

    /// <summary>
    /// Information about the user who created the pull request.
    /// </summary>
    public IdentityRef? CreatedBy { get; init; }

    /// <summary>
    /// The date and time when the pull request was created.
    /// </summary>
    public DateTime? CreationDate { get; init; }

    /// <summary>
    /// Information about the last merge commit.
    /// </summary>
    public CommitRef? LastMergeCommit { get; init; }
}

/// <summary>
/// Represents the status of a pull request.
/// </summary>
public enum PullRequestStatus
{
    /// <summary>
    /// The pull request is active and open.
    /// </summary>
    Active,

    /// <summary>
    /// The pull request has been completed/merged.
    /// </summary>
    Completed,

    /// <summary>
    /// The pull request has been abandoned.
    /// </summary>
    Abandoned
}

