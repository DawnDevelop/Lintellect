using Lintellect.Api.Application.Models.Git;

namespace Lintellect.Api.FunctionalTests.Mocks.Git;

/// <summary>
/// Mock implementation of IGitClient for testing.
/// </summary>
public sealed class MockGitClient : IGitClient
{
    public Task<List<CheckPermissionResult>> HasSufficientPermissionsAsync(AnalysisRequest request)
    {
        return Task.FromResult(new List<CheckPermissionResult>
        {
            new(true, "Read")
        });
    }

    public Task<Dictionary<string, string>> GetPullRequestCompactDiffsAsync(
        string projectName, string repositoryName, int pullRequestId,
        int contextLines = 3, int maxNewFileLines = 50, int maxLinesPerFile = 1000)
    {
        return Task.FromResult(new Dictionary<string, string>
        {
            ["TestFile.cs"] = "Mock diff content"
        });
    }

    public Task<Dictionary<string, string>> GetPullRequestFileDiffsAsync(
        string projectName, string repositoryName, int pullRequestId)
    {
        return Task.FromResult(new Dictionary<string, string>
        {
            ["TestFile.cs"] = "Mock full file diff"
        });
    }

    public Task<PullRequest> GetPullRequestAsync(string projectName, string repositoryName, int pullRequestId)
    {
        return Task.FromResult(new PullRequest
        {
            PullRequestId = pullRequestId,
            Title = "Test PR",
            Description = "Test description",
            Status = Application.Models.Git.PullRequestStatus.Active
        });
    }

    public Task<string?> GetFileAsync(string projectName, string repositoryName, string? branchName = null, params string[] possiblePaths)
    {
        return Task.FromResult<string?>("Mock file content");
    }

    public Task<PullRequestCommentThread> CreateCommentAsync(string projectName, string repositoryName, int pullRequestId, string comment)
    {
        return Task.FromResult(new PullRequestCommentThread
        {
            Id = 1,
            Comments = [new PullRequestComment { Id = 1, Content = comment }]
        });
    }

    public Task<PullRequestCommentThread> CreateCodeChangeCommentAsync(
        string projectName, string repositoryName, int pullRequestId, string codeChange,
        string? context = null, string? filePath = null, int? lineFrom = null, int? lineTo = null)
    {
        return Task.FromResult(new PullRequestCommentThread
        {
            Id = 1,
            Comments = [new PullRequestComment { Id = 1, Content = codeChange }]
        });
    }

    public Task<PullRequest> AppendToDescriptionAsync(
        string projectName, string repositoryName, int pullRequestId, string textToAppend, string separator = "\n\n---\n\n")
    {
        return Task.FromResult(new PullRequest
        {
            PullRequestId = pullRequestId,
            Description = "Updated description",
            Status = Application.Models.Git.PullRequestStatus.Active
        });
    }

    public Task AddCodeOwnersToPr(string projectName, int pullRequestId, CodeOwnersResult codeOwners, string? repositoryName = null)
    {
        return Task.CompletedTask;
    }



    public Task<PullRequestCommentThread> GetPullRequestThreadContextAsync(string projectName, string repositoryName, int pullRequestId, int prCommentId)
    {
        return Task.FromResult(new PullRequestCommentThread
        {
            Id = prCommentId,
            Comments = [new PullRequestComment { Id = 1, Content = "Mock comment" }]
        });
    }
}
