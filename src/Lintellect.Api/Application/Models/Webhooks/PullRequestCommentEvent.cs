using System.Text.Json.Serialization;

namespace Lintellect.Api.Application.Models.Webhooks;


public record PullRequestCommentEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("publisherId")] string PublisherId,
    [property: JsonPropertyName("message")] PullRequestCommentMessage Message,
    [property: JsonPropertyName("detailedMessage")] PullRequestCommentDetailedMessage DetailedMessage,
    [property: JsonPropertyName("resource")] PullRequestCommentResource Resource,
    [property: JsonPropertyName("resourceVersion")] string ResourceVersion,
    [property: JsonPropertyName("resourceContainers")] PullRequestCommentResourceContainers ResourceContainers,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate
);

public record PullRequestCommentAccount(
        [property: JsonPropertyName("id")] string Id
    );

public record PullRequestCommentAuthor(
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("imageUrl")] string ImageUrl
);

public record PullRequestCommentCollection(
    [property: JsonPropertyName("id")] string Id
);

public record PullRequestCommentComment(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("parentCommentId")] int ParentCommentId,
    [property: JsonPropertyName("author")] PullRequestCommentAuthor Author,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("publishedDate")] DateTime PublishedDate,
    [property: JsonPropertyName("lastUpdatedDate")] DateTime LastUpdatedDate,
    [property: JsonPropertyName("lastContentUpdatedDate")] DateTime LastContentUpdatedDate,
    [property: JsonPropertyName("commentType")] string CommentType,
    [property: JsonPropertyName("_links")] PullRequestCommentLinks Links
);

public record PullRequestCommentCommit(
    [property: JsonPropertyName("commitId")] string CommitId,
    [property: JsonPropertyName("url")] string Url
);

public record PullRequestCommentCreatedBy(
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("imageUrl")] string ImageUrl
);

public record PullRequestCommentDetailedMessage(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("markdown")] string Markdown
);

public record PullRequestCommentLastMergeCommit(
    [property: JsonPropertyName("commitId")] string CommitId,
    [property: JsonPropertyName("url")] string Url
);

public record PullRequestCommentLastMergeSourceCommit(
    [property: JsonPropertyName("commitId")] string CommitId,
    [property: JsonPropertyName("url")] string Url
);

public record PullRequestCommentLastMergeTargetCommit(
    [property: JsonPropertyName("commitId")] string CommitId,
    [property: JsonPropertyName("url")] string Url
);

public record PullRequestCommentLinks(
    [property: JsonPropertyName("self")] PullRequestCommentSelf Self,
    [property: JsonPropertyName("repository")] PullRequestCommentRepository Repository,
    [property: JsonPropertyName("threads")] PullRequestCommentThreads Threads,
    [property: JsonPropertyName("web")] PullRequestCommentWeb Web,
    [property: JsonPropertyName("statuses")] PullRequestCommentStatuses Statuses
);

public record PullRequestCommentMessage(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("markdown")] string Markdown
);

public record PullRequestCommentProject(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("visibility")] string Visibility,
    [property: JsonPropertyName("lastUpdateTime")] DateTime LastUpdateTime
);

public record PullRequestCommentPullRequest(
    [property: JsonPropertyName("repository")] PullRequestCommentRepository Repository,
    [property: JsonPropertyName("pullRequestId")] int PullRequestId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdBy")] PullRequestCommentCreatedBy CreatedBy,
    [property: JsonPropertyName("creationDate")] DateTime CreationDate,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("sourceRefName")] string SourceRefName,
    [property: JsonPropertyName("targetRefName")] string TargetRefName,
    [property: JsonPropertyName("mergeStatus")] string MergeStatus,
    [property: JsonPropertyName("mergeId")] string MergeId,
    [property: JsonPropertyName("lastMergeSourceCommit")] PullRequestCommentLastMergeSourceCommit LastMergeSourceCommit,
    [property: JsonPropertyName("lastMergeTargetCommit")] PullRequestCommentLastMergeTargetCommit LastMergeTargetCommit,
    [property: JsonPropertyName("lastMergeCommit")] PullRequestCommentLastMergeCommit LastMergeCommit,
    [property: JsonPropertyName("reviewers")] IReadOnlyList<PullRequestCommentReviewer> Reviewers,
    [property: JsonPropertyName("commits")] IReadOnlyList<PullRequestCommentCommit> Commits,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("_links")] PullRequestCommentLinks Links
);

public record PullRequestCommentRepository(
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("project")] PullRequestCommentProject Project,
    [property: JsonPropertyName("defaultBranch")] string DefaultBranch,
    [property: JsonPropertyName("remoteUrl")] string RemoteUrl
);

public record PullRequestCommentResource(
    [property: JsonPropertyName("comment")] PullRequestCommentComment Comment,
    [property: JsonPropertyName("pullRequest")] PullRequestCommentPullRequest PullRequest
);

public record PullRequestCommentResourceContainers(
    [property: JsonPropertyName("collection")] PullRequestCommentCollection Collection,
    [property: JsonPropertyName("account")] PullRequestCommentAccount Account,
    [property: JsonPropertyName("project")] PullRequestCommentProject Project
);

public record PullRequestCommentReviewer(
    [property: JsonPropertyName("reviewerUrl")] object ReviewerUrl,
    [property: JsonPropertyName("vote")] int Vote,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("imageUrl")] string ImageUrl,
    [property: JsonPropertyName("isContainer")] bool IsContainer
);

public record PullRequestCommentSelf(
    [property: JsonPropertyName("href")] string Href
);

public record PullRequestCommentStatuses(
    [property: JsonPropertyName("href")] string Href
);

public record PullRequestCommentThreads(
    [property: JsonPropertyName("href")] string Href
);

public record PullRequestCommentWeb(
    [property: JsonPropertyName("href")] string Href
);
