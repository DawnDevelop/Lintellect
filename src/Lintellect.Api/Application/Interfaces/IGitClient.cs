using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Abstraction for Git provider clients (Azure DevOps, GitHub, etc.).
/// Provides methods for retrieving pull request diffs with token optimization.
/// </summary>
public interface IGitClient
{
    /// <summary>
    /// Retrieves compact pull request diffs optimized for AI analysis.
    /// Only includes changed hunks with context, limits large files.
    /// </summary>
    /// <param name="projectName">Project/Organization name.</param>
    /// <param name="repositoryName">Repository name.</param>
    /// <param name="pullRequestId">Pull request ID/number.</param>
    /// <param name="contextLines">Number of context lines around changes.</param>
    /// <param name="maxNewFileLines">Maximum lines to show for new/deleted files.</param>
    /// <param name="maxLinesPerFile">Maximum total lines per file diff.</param>
    /// <returns>Dictionary mapping file paths to their compact diff content.</returns>
    Task<Dictionary<string, string>> GetPullRequestCompactDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        int contextLines);

    /// <summary>
    /// Retrieves full pull request diffs (entire file contents).
    /// Warning: Can result in high token usage for large files.
    /// </summary>
    Task<Dictionary<string, string>> GetPullRequestFileDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId);

    /// <summary>
    /// Retrieves pull request metadata.
    /// </summary>
    Task<PullRequest> GetPullRequestAsync(
        string projectName,
        string repositoryName,
        int pullRequestId);

    /// <summary>
    /// Retrieves custom instructions from the copilot-instructions.md file in the repository.
    /// Searches common locations: root, .github/, docs/, and .copilot/ directories.
    /// </summary>
    /// <param name="projectName">The name or ID of the project/organization.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="branchName">The branch name (e.g., "main", "master"). If null, uses the default branch.</param>
    /// <returns>The content of the copilot-instructions.md file, or null if not found.</returns>
    Task<string?> GetFileAsync(
        string projectName,
        string repositoryName,
        string? branchName = null,
        params string[] possiblePaths);

    /// <summary>
    /// Creates a new comment thread on a pull request.
    /// </summary>
    /// <param name="projectName">The name or ID of the project/organization.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <param name="comment">The comment text (supports Markdown).</param>
    /// <returns>The created comment thread.</returns>
    Task<PullRequestCommentThread> CreateCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string comment,
        int? threadId = null);

    /// <summary>
    /// Creates a new code change suggestion comment on a pull request.
    /// </summary>
    /// <param name="projectName">The name or ID of the project/organization.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <param name="codeChange">The suggested code change.</param>
    /// <param name="context">Optional: Additional context like titles, headers etc. (supports Markdown).</param>
    /// <param name="filePath">Optional: File path for inline comment. If null, creates a general PR comment.</param>
    /// <param name="lineFrom">Optional: Starting line number for inline comment (requires filePath).</param>
    /// <param name="lineTo">Optional: Ending line number for inline comment. If null, defaults to lineFrom.</param>
    /// <returns>The created comment thread.</returns>
    Task<PullRequestCommentThread> CreateCodeChangeCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string codeChange,
        string? context = null,
        string? filePath = null,
        int? lineFrom = null,
        int? lineTo = null);

    /// <summary>
    /// Updates the pull request description by appending text.
    /// Preserves existing description and adds the new content at the end.
    /// </summary>
    /// <param name="projectName">The name or ID of the project/organization.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <param name="textToAppend">The text to append to the description (supports Markdown).</param>
    /// <param name="separator">Optional separator between existing description and new text (default: double newline).</param>
    /// <returns>The updated pull request.</returns>
    Task<PullRequest> AppendToDescriptionAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string textToAppend,
        string separator = "\n\n---\n\n");


    /// <summary>
    /// Adds the specified code owners as reviewers to a pull request in the given project and repository.
    /// </summary>
    /// <param name="projectName">The name of the project containing the pull request.</param>
    /// <param name="pullRequestId">The unique identifier of the pull request to which reviewers will be added.</param>
    /// <param name="codeOwners">A list of code owner objects representing the code owners to be added as reviewers.</param>
    /// <param name="repositoryName">The name of the repository containing the pull request. If null, the default repository for the project is used.</param>
    /// <returns>A task that represents the asynchronous operation of adding code owners as reviewers to the pull request.</returns>
    Task AddCodeOwnersToPr(
        string projectName,
        int pullRequestId,
        CodeOwnersResult codeOwners,
        string? repositoryName = null);

    /// <summary>
    /// Checks if the current user has sufficient permissions for the analysis request.
    /// </summary>
    /// <param name="analysisRequest">The analysis request containing Git information and credentials.</param>
    /// <returns>A task that represents the asynchronous operation to check permissions, returning detailed permission results for each required permission.</returns>
    Task<List<CheckPermissionResult>> HasSufficientPermissionsAsync(AnalysisRequest analysisRequest);


    /// <summary>
    /// Retrieves the context (thread and details) of a specific pull request comment thread.
    /// </summary>
    /// <param name="projectName">The name or ID of the project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The unique identifier of the pull request.</param>
    /// <param name="prCommentId">The unique identifier of the pull request comment thread.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// the <see cref="PullRequestCommentThread"/> with details of the specified comment thread.
    /// </returns>
    Task<PullRequestCommentThread> GetPullRequestThreadContextAsync(string projectName, string repositoryName, int pullRequestId, int prCommentId);

}
