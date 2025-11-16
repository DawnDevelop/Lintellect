namespace Lintellect.Api.Application.Models.Git;

/// <summary>
/// Represents a commit reference from any Git provider.
/// This is a provider-agnostic model following Clean Architecture principles.
/// </summary>
public sealed class CommitRef
{
    /// <summary>
    /// The commit SHA/hash identifier.
    /// </summary>
    public string? CommitId { get; init; }

    /// <summary>
    /// The commit message.
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// The author of the commit.
    /// </summary>
    public IdentityRef? Author { get; init; }

    /// <summary>
    /// The committer of the commit.
    /// </summary>
    public IdentityRef? Committer { get; init; }

    /// <summary>
    /// The date and time when the commit was created.
    /// </summary>
    public DateTime? CommitDate { get; init; }
}

