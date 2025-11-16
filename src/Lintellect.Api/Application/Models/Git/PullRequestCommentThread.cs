namespace Lintellect.Api.Application.Models.Git;

/// <summary>
/// Represents a comment thread on a pull request from any Git provider.
/// This is a provider-agnostic model following Clean Architecture principles.
/// </summary>
public sealed class PullRequestCommentThread
{
    /// <summary>
    /// The unique identifier of the comment thread.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// The list of comments in this thread.
    /// </summary>
    public required IList<PullRequestComment> Comments { get; init; }

    /// <summary>
    /// The status of the comment thread (e.g., Active, Resolved, Closed).
    /// </summary>
    public CommentThreadStatus Status { get; init; } = CommentThreadStatus.Active;

    /// <summary>
    /// Optional context for inline comments (file path and line numbers).
    /// </summary>
    public CommentThreadContext? ThreadContext { get; init; }
}

/// <summary>
/// Represents the status of a comment thread.
/// </summary>
public enum CommentThreadStatus
{
    /// <summary>
    /// The thread is active and open.
    /// </summary>
    Active,

    /// <summary>
    /// The thread has been resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// The thread has been closed.
    /// </summary>
    Closed
}

/// <summary>
/// Represents the context for an inline comment (file path and line positions).
/// </summary>
public sealed class CommentThreadContext
{
    /// <summary>
    /// The file path where the comment is located.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The starting position of the comment (line and offset).
    /// </summary>
    public CommentPosition? RightFileStart { get; init; }

    /// <summary>
    /// The ending position of the comment (line and offset).
    /// </summary>
    public CommentPosition? RightFileEnd { get; init; }
}

/// <summary>
/// Represents a position in a file (line number and character offset).
/// </summary>
public sealed class CommentPosition
{
    /// <summary>
    /// The line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// The character offset within the line (1-based).
    /// </summary>
    public int Offset { get; init; } = 1;
}

