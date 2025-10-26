namespace Lintellect.Api.functionaltests.Utilities;

/// <summary>
/// Mock implementation of IGitClientFactory for testing.
/// </summary>
public sealed class MockGitClientFactory : IGitClientFactory
{
    public IGitClient CreateClient(AnalysisRequest request)
    {
        return new MockGitClient();
    }
}

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

    public Task<GitPullRequest> GetPullRequestAsync(string projectName, string repositoryName, int pullRequestId)
    {
        return Task.FromResult(new GitPullRequest
        {
            PullRequestId = pullRequestId,
            Title = "Test PR",
            Description = "Test description"
        });
    }

    public Task<string?> GetFileAsync(string projectName, string repositoryName, string? branchName = null, params string[] possiblePaths)
    {
        return Task.FromResult<string?>("Mock file content");
    }

    public Task<GitPullRequestCommentThread> CreateCommentAsync(string projectName, string repositoryName, int pullRequestId, string comment)
    {
        return Task.FromResult(new GitPullRequestCommentThread
        {
            Id = 1,
            Comments = [new() { Content = comment }]
        });
    }

    public Task<GitPullRequestCommentThread> CreateCodeChangeCommentAsync(
        string projectName, string repositoryName, int pullRequestId, string codeChange,
        string? context = null, string? filePath = null, int? lineFrom = null, int? lineTo = null)
    {
        return Task.FromResult(new GitPullRequestCommentThread
        {
            Id = 1,
            Comments = [new() { Content = codeChange }]
        });
    }

    public Task<GitPullRequest> AppendToDescriptionAsync(
        string projectName, string repositoryName, int pullRequestId, string textToAppend, string separator = "\n\n---\n\n")
    {
        return Task.FromResult(new GitPullRequest
        {
            PullRequestId = pullRequestId,
            Description = "Updated description"
        });
    }

    public Task AddCodeOwnersToPr(string projectName, int pullRequestId, CodeOwnersResult codeOwners, string? repositoryName = null)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock implementation of IAnalyzerServiceResolver for testing.
/// </summary>
public sealed class MockAnalyzerServiceResolver : IAnalyzerServiceResolver
{
    public IAnalyzerService GetAnalyzerService(EAnalyzers provider)
    {
        return new MockAnalyzerService();
    }
}

/// <summary>
/// Mock implementation of IAnalyzerService for testing.
/// </summary>
public sealed class MockAnalyzerService : IAnalyzerService
{
    public Task<string> AnalyzeAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Mock detailed analysis");
    }

    public Task<string> GenerateSummaryAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Mock summary");
    }

    public Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<InlineSuggestion>
        {
            new()
            {
                FilePath = "TestFile.cs",
                LineFrom = 10,
                Title = "Mock suggestion title",
                Explanation = "Mock suggestion explanation",
                SuggestedCode = "Mock suggested code"
            }
        });
    }

    public Task<CodeOwnersResult?> GetCodeOwnersAsync(string codeOwnerFileContent, List<string> changedFilePaths, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CodeOwnersResult?>(new CodeOwnersResult
        {
            CodeOwners =
            [
                new() { Name = "test@example.com", Type = CodeOwnerType.Email, Email = "test@example.com" }
            ]
        });
    }
}
