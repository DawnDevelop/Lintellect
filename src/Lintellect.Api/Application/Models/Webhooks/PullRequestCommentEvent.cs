namespace Lintellect.Api.Application.Models.Webhooks;

public record PullRequestCommentEvent(
    string Id,
    string EventType,
    string PublisherId,
    Message Message,
    DetailedMessage DetailedMessage,
    Resource Resource,
    string ResourceVersion,
    ResourceContainers ResourceContainers,
    DateTime CreatedDate
);


public record Author(
    string DisplayName,
    string Url,
    string Id,
    string UniqueName,
    string ImageUrl
);
public record Comment(
    int Id,
    int ParentCommentId,
    Author Author,
    string Content,
    DateTime PublishedDate,
    DateTime LastUpdatedDate,
    DateTime LastContentUpdatedDate,
    string CommentType,
    Links Links
);

public record Links(
    Self Self,
    Repository Repository,
    Threads Threads,
    Web Web,
    Statuses Statuses
);

public record PullRequest(
    Repository Repository,
    int PullRequestId,
    string Status,
    CreatedBy CreatedBy,
    DateTime CreationDate,
    string Title,
    string Description,
    string SourceRefName,
    string TargetRefName,
    string MergeStatus,
    string MergeId,
    LastMergeSourceCommit LastMergeSourceCommit,
    LastMergeTargetCommit LastMergeTargetCommit,
    LastMergeCommit LastMergeCommit,
    IReadOnlyList<Reviewer> Reviewers,
    IReadOnlyList<Commit> Commits,
    string Url,
    Links Links
);

public record Self(
    string Href
);

public record Statuses(
    string Href
);

public record Threads(
    string Href
);

public record Web(
    string Href
);


