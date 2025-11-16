using Lintellect.Api.Application.Models.Webhooks;

namespace Lintellect.Api.FunctionalTests.Utilities.Webhooks;

/// <summary>
/// Fluent builder for creating webhook test data.
/// </summary>
public sealed class WebhookTestDataBuilder
{
    private readonly PullRequestCommentEventBuilder _commentEventBuilder = new();
    private readonly PullRequestUpdatedEventBuilder _updateEventBuilder = new();

    public WebhookTestDataBuilder WithQuestionComment(string question)
    {
        _commentEventBuilder.WithCommentContent(question);
        return this;
    }

    public WebhookTestDataBuilder WithNonQuestionComment(string comment)
    {
        _commentEventBuilder.WithCommentContent(comment);
        return this;
    }

    public WebhookTestDataBuilder WithPullRequestId(int pullRequestId)
    {
        _commentEventBuilder.WithPullRequestId(pullRequestId);
        _updateEventBuilder.WithPullRequestId(pullRequestId);
        return this;
    }

    public WebhookTestDataBuilder WithRepositoryName(string repositoryName)
    {
        _commentEventBuilder.WithRepositoryName(repositoryName);
        _updateEventBuilder.WithRepositoryName(repositoryName);
        return this;
    }

    public WebhookTestDataBuilder WithProjectName(string projectName)
    {
        _commentEventBuilder.WithProjectName(projectName);
        _updateEventBuilder.WithProjectName(projectName);
        return this;
    }

    public PullRequestCommentEvent BuildCommentEvent()
    {
        return _commentEventBuilder.Build();
    }

    public PullRequestUpdatedEvent BuildUpdateEvent()
    {
        return _updateEventBuilder.Build();
    }

    public static PullRequestCommentEvent ValidQuestionCommentEvent()
    {
        return new WebhookTestDataBuilder()
            .WithQuestionComment("@lintellect What does this code do?")
            .WithPullRequestId(123)
            .WithRepositoryName("TestRepo")
            .WithProjectName("TestProject")
            .BuildCommentEvent();
    }

    public static PullRequestCommentEvent ValidNonQuestionCommentEvent()
    {
        return new WebhookTestDataBuilder()
            .WithNonQuestionComment("This looks good!")
            .WithPullRequestId(123)
            .WithRepositoryName("TestRepo")
            .WithProjectName("TestProject")
            .BuildCommentEvent();
    }

    public static PullRequestUpdatedEvent ValidUpdateEvent()
    {
        return new WebhookTestDataBuilder()
            .WithPullRequestId(123)
            .WithRepositoryName("TestRepo")
            .WithProjectName("TestProject")
            .BuildUpdateEvent();
    }
}

/// <summary>
/// Builder for PullRequestCommentEvent test data.
/// </summary>
internal sealed class PullRequestCommentEventBuilder
{
    private string _commentContent = "Test comment";
    private int _pullRequestId = 123;
    private string _repositoryName = "TestRepo";
    private string _projectName = "TestProject";
    private int _threadId = 1;

    public PullRequestCommentEventBuilder WithCommentContent(string content)
    {
        _commentContent = content;
        return this;
    }

    public PullRequestCommentEventBuilder WithPullRequestId(int pullRequestId)
    {
        _pullRequestId = pullRequestId;
        return this;
    }

    public PullRequestCommentEventBuilder WithRepositoryName(string repositoryName)
    {
        _repositoryName = repositoryName;
        return this;
    }

    public PullRequestCommentEventBuilder WithProjectName(string projectName)
    {
        _projectName = projectName;
        return this;
    }

    public PullRequestCommentEventBuilder WithThreadId(int threadId)
    {
        _threadId = threadId;
        return this;
    }

    public PullRequestCommentEvent Build()
    {
        // Generate a 40-character commit ID (SHA-1 format)
        var commitId = (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..40];
        var authorId = Guid.NewGuid().ToString();

        return new PullRequestCommentEvent(
            Id: Guid.NewGuid().ToString(),
            EventType: "git.pullrequest.comment.created",
            PublisherId: "tfs",
            Message: new PullRequestCommentMessage("Comment added", "<p>Comment added</p>", "Comment added"),
            DetailedMessage: new PullRequestCommentDetailedMessage("Comment added", "<p>Comment added</p>", "Comment added"),
            Resource: new PullRequestCommentResource(
                Comment: new PullRequestCommentComment(
                    Id: 1,
                    ParentCommentId: 0,
                    Author: new PullRequestCommentAuthor(
                        DisplayName: "Test User",
                        Url: $"https://dev.azure.com/test/{authorId}",
                        Id: authorId,
                        UniqueName: "test@example.com",
                        ImageUrl: "https://dev.azure.com/test/avatar.png"
                    ),
                    Content: _commentContent,
                    PublishedDate: DateTime.UtcNow,
                    LastUpdatedDate: DateTime.UtcNow,
                    LastContentUpdatedDate: DateTime.UtcNow,
                    CommentType: "text",
                    Links: new PullRequestCommentLinks(
                        Self: new PullRequestCommentSelf($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}/threads/{_threadId}/comments/1"),
                        Repository: new PullRequestCommentRepository(
                            Href: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}",
                            Id: Guid.NewGuid().ToString(),
                            Name: _repositoryName,
                            Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}",
                            Project: new PullRequestCommentProject(
                                Id: Guid.NewGuid().ToString(),
                                Name: _projectName,
                                Url: $"https://dev.azure.com/test/{_projectName}",
                                State: "wellFormed",
                                Visibility: "private",
                                LastUpdateTime: DateTime.UtcNow
                            ),
                            DefaultBranch: "refs/heads/main",
                            RemoteUrl: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}"
                        ),
                        Threads: new PullRequestCommentThreads($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}/threads/{_threadId}"),
                        Web: new PullRequestCommentWeb($"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}/pullrequest/{_pullRequestId}"),
                        Statuses: new PullRequestCommentStatuses($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}/statuses")
                    )
                ),
                PullRequest: new PullRequestCommentPullRequest(
                    Repository: new PullRequestCommentRepository(
                        Href: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}",
                        Id: Guid.NewGuid().ToString(),
                        Name: _repositoryName,
                        Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}",
                        Project: new PullRequestCommentProject(
                            Id: Guid.NewGuid().ToString(),
                            Name: _projectName,
                            Url: $"https://dev.azure.com/test/{_projectName}",
                            State: "wellFormed",
                            Visibility: "private",
                            LastUpdateTime: DateTime.UtcNow
                        ),
                        DefaultBranch: "refs/heads/main",
                        RemoteUrl: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}"
                    ),
                    PullRequestId: _pullRequestId,
                    Status: "active",
                    CreatedBy: new PullRequestCommentCreatedBy(
                        DisplayName: "Test User",
                        Url: $"https://dev.azure.com/test/{authorId}",
                        Id: authorId,
                        UniqueName: "test@example.com",
                        ImageUrl: "https://dev.azure.com/test/avatar.png"
                    ),
                    CreationDate: DateTime.UtcNow,
                    Title: "Test Pull Request",
                    Description: "Test description",
                    SourceRefName: "refs/heads/feature/test",
                    TargetRefName: "refs/heads/main",
                    MergeStatus: "succeeded",
                    MergeId: Guid.NewGuid().ToString(),
                    LastMergeSourceCommit: new PullRequestCommentLastMergeSourceCommit(
                        CommitId: commitId,
                        Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{commitId}"
                    ),
                    LastMergeTargetCommit: new PullRequestCommentLastMergeTargetCommit(
                        CommitId: (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..40],
                        Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{Guid.NewGuid()}"
                    ),
                    LastMergeCommit: new PullRequestCommentLastMergeCommit(
                        CommitId: commitId,
                        Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{commitId}"
                    ),
                    Reviewers: [],
                    Commits: [],
                    Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}/pullrequest/{_pullRequestId}",
                    Links: new PullRequestCommentLinks(
                        Self: new PullRequestCommentSelf($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}"),
                        Repository: new PullRequestCommentRepository(
                            Href: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}",
                            Id: Guid.NewGuid().ToString(),
                            Name: _repositoryName,
                            Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}",
                            Project: new PullRequestCommentProject(
                                Id: Guid.NewGuid().ToString(),
                                Name: _projectName,
                                Url: $"https://dev.azure.com/test/{_projectName}",
                                State: "wellFormed",
                                Visibility: "private",
                                LastUpdateTime: DateTime.UtcNow
                            ),
                            DefaultBranch: "refs/heads/main",
                            RemoteUrl: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}"
                        ),
                        Threads: new PullRequestCommentThreads($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}/threads/{_threadId}"),
                        Web: new PullRequestCommentWeb($"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}/pullrequest/{_pullRequestId}"),
                        Statuses: new PullRequestCommentStatuses($"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/pullRequests/{_pullRequestId}/statuses")
                    )
                )
            ),
            ResourceVersion: "1.0",
            ResourceContainers: new PullRequestCommentResourceContainers(
                Collection: new PullRequestCommentCollection(Guid.NewGuid().ToString()),
                Account: new PullRequestCommentAccount(Guid.NewGuid().ToString()),
                Project: new PullRequestCommentProject(
                    Id: Guid.NewGuid().ToString(),
                    Name: _projectName,
                    Url: $"https://dev.azure.com/test/{_projectName}",
                    State: "wellFormed",
                    Visibility: "private",
                    LastUpdateTime: DateTime.UtcNow
                )
            ),
            CreatedDate: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Builder for PullRequestUpdatedEvent test data.
/// </summary>
internal sealed class PullRequestUpdatedEventBuilder
{
    private int _pullRequestId = 123;
    private string _repositoryName = "TestRepo";
    private string _projectName = "TestProject";

    public PullRequestUpdatedEventBuilder WithPullRequestId(int pullRequestId)
    {
        _pullRequestId = pullRequestId;
        return this;
    }

    public PullRequestUpdatedEventBuilder WithRepositoryName(string repositoryName)
    {
        _repositoryName = repositoryName;
        return this;
    }

    public PullRequestUpdatedEventBuilder WithProjectName(string projectName)
    {
        _projectName = projectName;
        return this;
    }

    public PullRequestUpdatedEvent Build()
    {
        // Generate a 40-character commit ID (SHA-1 format)
        var commitId = (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..40];
        var authorId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var repositoryId = Guid.NewGuid().ToString();

        return new PullRequestUpdatedEvent(
            Id: Guid.NewGuid().ToString(),
            EventType: "git.pullrequest.updated",
            PublisherId: "tfs",
            Scope: "all",
            Message: new PullRequestUpdatedMessage(
                Text: "Pull request updated",
                Html: "<p>Pull request updated</p>",
                Markdown: "Pull request updated"
            ),
            DetailedMessage: new PullRequestUpdatedDetailedMessage(
                Text: "Pull request updated",
                Html: "<p>Pull request updated</p>",
                Markdown: "Pull request updated"
            ),
            Resource: new PullRequestUpdatedResource(
                Repository: new PullRequestUpdatedRepository(
                    Id: repositoryId,
                    Name: _repositoryName,
                    Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}",
                    Project: new PullRequestUpdatedProject(
                        Id: projectId,
                        Name: _projectName,
                        Url: $"https://dev.azure.com/test/{_projectName}",
                        State: "wellFormed"
                    ),
                    DefaultBranch: "refs/heads/main",
                    RemoteUrl: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}"
                ),
                PullRequestId: _pullRequestId,
                Status: "active",
                CreatedBy: new PullRequestUpdatedCreatedBy(
                    Id: authorId,
                    DisplayName: "Test User",
                    UniqueName: "test@example.com",
                    Url: $"https://dev.azure.com/test/{authorId}",
                    ImageUrl: "https://dev.azure.com/test/avatar.png"
                ),
                CreationDate: DateTime.UtcNow,
                ClosedDate: DateTime.MinValue,
                Title: "Test Pull Request",
                Description: "Test description",
                SourceRefName: "refs/heads/feature/test",
                TargetRefName: "refs/heads/main",
                MergeStatus: "succeeded",
                MergeId: Guid.NewGuid().ToString(),
                LastMergeSourceCommit: new PullRequestUpdatedLastMergeSourceCommit(
                    CommitId: commitId,
                    Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{commitId}"
                ),
                LastMergeTargetCommit: new PullRequestUpdatedLastMergeTargetCommit(
                    CommitId: (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..40],
                    Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{Guid.NewGuid()}"
                ),
                LastMergeCommit: new PullRequestUpdatedLastMergeCommit(
                    CommitId: commitId,
                    Url: $"https://dev.azure.com/test/_apis/git/repositories/{_repositoryName}/commits/{commitId}"
                ),
                Reviewers: [],
                Commits: [],
                Url: $"https://dev.azure.com/test/{_projectName}/_git/{_repositoryName}/pullrequest/{_pullRequestId}"
            ),
            ResourceVersion: "1.0",
            ResourceContainers: new PullRequestUpdatedResourceContainers(
                Collection: new PullRequestUpdatedCollection(Guid.NewGuid().ToString()),
                Account: new PullRequestUpdatedAccount(Guid.NewGuid().ToString()),
                Project: new PullRequestUpdatedProject(
                    Id: projectId,
                    Name: _projectName,
                    Url: $"https://dev.azure.com/test/{_projectName}",
                    State: "wellFormed"
                )
            ),
            CreatedDate: DateTime.UtcNow
        );
    }
}

