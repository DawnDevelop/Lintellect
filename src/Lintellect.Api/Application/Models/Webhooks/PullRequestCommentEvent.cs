using Newtonsoft.Json;

namespace Lintellect.Api.Application.Models.Webhooks;


public record PullRequestCommentEvent(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("eventType")] string EventType,
    [property: JsonProperty("publisherId")] string PublisherId,
    [property: JsonProperty("message")] PullRequestCommentMessage Message,
    [property: JsonProperty("detailedMessage")] PullRequestCommentDetailedMessage DetailedMessage,
    [property: JsonProperty("resource")] PullRequestCommentResource Resource,
    [property: JsonProperty("resourceVersion")] string ResourceVersion,
    [property: JsonProperty("resourceContainers")] PullRequestCommentResourceContainers ResourceContainers,
    [property: JsonProperty("createdDate")] DateTime CreatedDate
);

public record PullRequestCommentAccount(
        [property: JsonProperty("id")] string Id
    );

public record PullRequestCommentAuthor(
    [property: JsonProperty("displayName")] string DisplayName,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("uniqueName")] string UniqueName,
    [property: JsonProperty("imageUrl")] string ImageUrl
);

public record PullRequestCommentCollection(
    [property: JsonProperty("id")] string Id
);

public record PullRequestCommentComment(
    [property: JsonProperty("id")] int Id,
    [property: JsonProperty("parentCommentId")] int ParentCommentId,
    [property: JsonProperty("author")] PullRequestCommentAuthor Author,
    [property: JsonProperty("content")] string Content,
    [property: JsonProperty("publishedDate")] DateTime PublishedDate,
    [property: JsonProperty("lastUpdatedDate")] DateTime LastUpdatedDate,
    [property: JsonProperty("lastContentUpdatedDate")] DateTime LastContentUpdatedDate,
    [property: JsonProperty("commentType")] string CommentType,
    [property: JsonProperty("_links")] PullRequestCommentLinks Links
);

public record PullRequestCommentCommit(
    [property: JsonProperty("commitId")] string CommitId,
    [property: JsonProperty("url")] string Url
);

public record PullRequestCommentCreatedBy(
    [property: JsonProperty("displayName")] string DisplayName,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("uniqueName")] string UniqueName,
    [property: JsonProperty("imageUrl")] string ImageUrl
);

public record PullRequestCommentDetailedMessage(
    [property: JsonProperty("text")] string Text,
    [property: JsonProperty("html")] string Html,
    [property: JsonProperty("markdown")] string Markdown
);

public record PullRequestCommentLastMergeCommit(
    [property: JsonProperty("commitId")] string CommitId,
    [property: JsonProperty("url")] string Url
);

public record PullRequestCommentLastMergeSourceCommit(
    [property: JsonProperty("commitId")] string CommitId,
    [property: JsonProperty("url")] string Url
);

public record PullRequestCommentLastMergeTargetCommit(
    [property: JsonProperty("commitId")] string CommitId,
    [property: JsonProperty("url")] string Url
);

public record PullRequestCommentLinks(
    [property: JsonProperty("self")] PullRequestCommentSelf Self,
    [property: JsonProperty("repository")] PullRequestCommentRepository Repository,
    [property: JsonProperty("threads")] PullRequestCommentThreads Threads,
    [property: JsonProperty("web")] PullRequestCommentWeb Web,
    [property: JsonProperty("statuses")] PullRequestCommentStatuses Statuses
);

public record PullRequestCommentMessage(
    [property: JsonProperty("text")] string Text,
    [property: JsonProperty("html")] string Html,
    [property: JsonProperty("markdown")] string Markdown
);

public record PullRequestCommentProject(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("state")] string State,
    [property: JsonProperty("visibility")] string Visibility,
    [property: JsonProperty("lastUpdateTime")] DateTime LastUpdateTime
);

public record PullRequestCommentPullRequest(
    [property: JsonProperty("repository")] PullRequestCommentRepository Repository,
    [property: JsonProperty("pullRequestId")] int PullRequestId,
    [property: JsonProperty("status")] string Status,
    [property: JsonProperty("createdBy")] PullRequestCommentCreatedBy CreatedBy,
    [property: JsonProperty("creationDate")] DateTime CreationDate,
    [property: JsonProperty("title")] string Title,
    [property: JsonProperty("description")] string Description,
    [property: JsonProperty("sourceRefName")] string SourceRefName,
    [property: JsonProperty("targetRefName")] string TargetRefName,
    [property: JsonProperty("mergeStatus")] string MergeStatus,
    [property: JsonProperty("mergeId")] string MergeId,
    [property: JsonProperty("lastMergeSourceCommit")] PullRequestCommentLastMergeSourceCommit LastMergeSourceCommit,
    [property: JsonProperty("lastMergeTargetCommit")] PullRequestCommentLastMergeTargetCommit LastMergeTargetCommit,
    [property: JsonProperty("lastMergeCommit")] PullRequestCommentLastMergeCommit LastMergeCommit,
    [property: JsonProperty("reviewers")] IReadOnlyList<PullRequestCommentReviewer> Reviewers,
    [property: JsonProperty("commits")] IReadOnlyList<PullRequestCommentCommit> Commits,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("_links")] PullRequestCommentLinks Links
);

public record PullRequestCommentRepository(
    [property: JsonProperty("href")] string Href,
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("project")] PullRequestCommentProject Project,
    [property: JsonProperty("defaultBranch")] string DefaultBranch,
    [property: JsonProperty("remoteUrl")] string RemoteUrl
);

public record PullRequestCommentResource(
    [property: JsonProperty("comment")] PullRequestCommentComment Comment,
    [property: JsonProperty("pullRequest")] PullRequestCommentPullRequest PullRequest
);

public record PullRequestCommentResourceContainers(
    [property: JsonProperty("collection")] PullRequestCommentCollection Collection,
    [property: JsonProperty("account")] PullRequestCommentAccount Account,
    [property: JsonProperty("project")] PullRequestCommentProject Project
);

public record PullRequestCommentReviewer(
    [property: JsonProperty("reviewerUrl")] object ReviewerUrl,
    [property: JsonProperty("vote")] int Vote,
    [property: JsonProperty("displayName")] string DisplayName,
    [property: JsonProperty("url")] string Url,
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("uniqueName")] string UniqueName,
    [property: JsonProperty("imageUrl")] string ImageUrl,
    [property: JsonProperty("isContainer")] bool IsContainer
);

public record PullRequestCommentSelf(
    [property: JsonProperty("href")] string Href
);

public record PullRequestCommentStatuses(
    [property: JsonProperty("href")] string Href
);

public record PullRequestCommentThreads(
    [property: JsonProperty("href")] string Href
);

public record PullRequestCommentWeb(
    [property: JsonProperty("href")] string Href
);
