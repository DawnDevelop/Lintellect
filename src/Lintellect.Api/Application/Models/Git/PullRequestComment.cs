namespace Lintellect.Api.Application.Models.Git;

/// <summary>
/// Represents a comment on a pull request from any Git provider.
/// This is a provider-agnostic model following Clean Architecture principles.
/// </summary>
public sealed class PullRequestComment
{
    /// <summary>
    /// The unique identifier of the comment.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// The identifier of the parent comment, if this is a reply.
    /// </summary>
    public int? ParentCommentId { get; init; }

    /// <summary>
    /// The content/body of the comment (supports Markdown).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The author of the comment.
    /// </summary>
    public IdentityRef? Author { get; init; }

    /// <summary>
    /// The date and time when the comment was published.
    /// </summary>
    public DateTime? PublishedDate { get; init; }

    /// <summary>
    /// The date and time when the comment was last updated.
    /// </summary>
    public DateTime? LastUpdatedDate { get; init; }

    /// <summary>
    /// The type of comment (e.g., Text, CodeChange, System).
    /// </summary>
    public CommentType CommentType { get; init; } = CommentType.Text;
}

/// <summary>
/// Represents the type of a comment.
/// </summary>
public enum CommentType
{
    /// <summary>
    /// A regular text comment.
    /// </summary>
    Text,

    /// <summary>
    /// A code change suggestion comment.
    /// </summary>
    CodeChange,

    /// <summary>
    /// A system-generated comment.
    /// </summary>
    System
}

