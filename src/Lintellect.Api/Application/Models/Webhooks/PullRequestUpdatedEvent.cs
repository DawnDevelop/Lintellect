namespace Lintellect.Api.Application.Models.Webhooks;



public record PullRequestUpdatedEvent(
    string Id,
    string EventType,
    string PublisherId,
    string Scope,
    PullRequestUpdatedMessage Message,
    PullRequestUpdatedDetailedMessage DetailedMessage,
    PullRequestUpdatedResource Resource,
    string ResourceVersion,
    PullRequestUpdatedResourceContainers ResourceContainers,
    DateTime CreatedDate
);


public record PullRequestUpdatedAccount(
    string Id
);

public record PullRequestUpdatedCollection(
    string Id
);

public record PullRequestUpdatedCommit(
    string CommitId,
    string Url
);

public record PullRequestUpdatedCreatedBy(
    string Id,
    string DisplayName,
    string UniqueName,
    string Url,
    string ImageUrl
);

public record PullRequestUpdatedDetailedMessage(
    string Text,
    string Html,
    string Markdown
);

public record PullRequestUpdatedLastMergeCommit(
    string CommitId,
    string Url
);

public record PullRequestUpdatedLastMergeSourceCommit(
    string CommitId,
    string Url
);

public record PullRequestUpdatedLastMergeTargetCommit(
    string CommitId,
    string Url
);

public record PullRequestUpdatedMessage(
    string Text,
    string Html,
    string Markdown
);

public record PullRequestUpdatedProject(
    string Id,
    string Name,
    string Url,
    string State
);

public record PullRequestUpdatedRepository(
    string Id,
    string Name,
    string Url,
    PullRequestUpdatedProject Project,
    string DefaultBranch,
    string RemoteUrl
);

public record PullRequestUpdatedResource(
    PullRequestUpdatedRepository Repository,
    int PullRequestId,
    string Status,
    PullRequestUpdatedCreatedBy CreatedBy,
    DateTime CreationDate,
    DateTime ClosedDate,
    string Title,
    string Description,
    string SourceRefName,
    string TargetRefName,
    string MergeStatus,
    string MergeId,
    PullRequestUpdatedLastMergeSourceCommit LastMergeSourceCommit,
    PullRequestUpdatedLastMergeTargetCommit LastMergeTargetCommit,
    PullRequestUpdatedLastMergeCommit LastMergeCommit,
    IReadOnlyList<PullRequestUpdatedReviewer> Reviewers,
    IReadOnlyList<PullRequestUpdatedCommit> Commits,
    string Url
);

public record PullRequestUpdatedResourceContainers(
    PullRequestUpdatedCollection Collection,
    PullRequestUpdatedAccount Account,
    PullRequestUpdatedProject Project
);

public record PullRequestUpdatedReviewer(
    object ReviewerUrl,
    int Vote,
    string Id,
    string DisplayName,
    string UniqueName,
    string Url,
    string ImageUrl,
    bool IsContainer
);


