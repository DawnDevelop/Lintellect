namespace Lintellect.Api.Application.Models.Webhooks;



public record PullRequestUpdatedEvent(
    string Id,
    string EventType,
    string PublisherId,
    string Scope,
    Message Message,
    DetailedMessage DetailedMessage,
    Resource Resource,
    string ResourceVersion,
    ResourceContainers ResourceContainers,
    DateTime CreatedDate
);


public record Account(
    string Id
);

public record Collection(
    string Id
);

public record Commit(
    string CommitId,
    string Url
);

public record CreatedBy(
    string Id,
    string DisplayName,
    string UniqueName,
    string Url,
    string ImageUrl
);

public record DetailedMessage(
    string Text,
    string Html,
    string Markdown
);

public record LastMergeCommit(
    string CommitId,
    string Url
);

public record LastMergeSourceCommit(
    string CommitId,
    string Url
);

public record LastMergeTargetCommit(
    string CommitId,
    string Url
);

public record Message(
    string Text,
    string Html,
    string Markdown
);

public record Project(
    string Id,
    string Name,
    string Url,
    string State
);

public record Repository(
    string Id,
    string Name,
    string Url,
    Project Project,
    string DefaultBranch,
    string RemoteUrl
);

public record Resource(
    Repository Repository,
    int PullRequestId,
    string Status,
    CreatedBy CreatedBy,
    DateTime CreationDate,
    DateTime ClosedDate,
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
    string Url
);

public record ResourceContainers(
    Collection Collection,
    Account Account,
    Project Project
);

public record Reviewer(
    object ReviewerUrl,
    int Vote,
    string Id,
    string DisplayName,
    string UniqueName,
    string Url,
    string ImageUrl,
    bool IsContainer
);


