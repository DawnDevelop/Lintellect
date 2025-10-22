using devops_pr_analyzer.Interfaces;
using devops_pr_analyzer.shared.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace devops_pr_analyzer.Services.Git;

/// <summary>
/// Service for interacting with pull requests from any Git provider.
/// Handles diffs, custom instructions, comments, and description updates with token optimization for AI analysis.
/// </summary>
public sealed class PullRequestService(IGitClientResolver clientResolver)
{
    private readonly IGitClientResolver _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));

    public IGitClientResolver ClientResolver => _clientResolver;

    /// <summary>
    /// Retrieves compact diffs for a pull request from an AnalysisResult.
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <param name="contextLines">Number of context lines around changes (default: 3).</param>
    /// <param name="maxNewFileLines">Maximum lines for new/deleted files (default: 50).</param>
    /// <param name="maxLinesPerFile">Maximum total lines per file (default: 1000).</param>
    /// <param name="filterByFindings">Only include files with analyzer findings (default: false).</param>
    /// <returns>Dictionary of file paths to compact diff content.</returns>
    /// <exception cref="InvalidOperationException">When PR information is invalid or missing.</exception>
    public async Task<Dictionary<string, string>> GetCompactDiffsAsync(
        AnalysisResult analysisResult,
        int contextLines = 3,
        int maxNewFileLines = 50,
        int maxLinesPerFile = 1000)
    {
        ValidateAnalysisResult(analysisResult);

        var (projectName, repositoryName, pullRequestId) = ExtractPullRequestInfo(analysisResult);
        
        // Get the appropriate client based on the provider
        var gitClient = _clientResolver.GetClient(analysisResult);

        var diffs = await gitClient.GetPullRequestCompactDiffsAsync(
            projectName,
            repositoryName,
            pullRequestId,
            contextLines,
            maxNewFileLines,
            maxLinesPerFile)
            .ConfigureAwait(false);

        return diffs;
    }

    /// <summary>
    /// Retrieves custom project-specific instructions from copilot-instructions.md file.
    /// Searches common locations in the target branch of the pull request.
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <returns>Custom instructions content, or null if not found.</returns>
    public async Task<string?> GetCustomInstructionsAsync(AnalysisResult analysisResult)
    {
        try
        {
            ValidateAnalysisResult(analysisResult);

            var (projectName, repositoryName, pullRequestId) = ExtractPullRequestInfo(analysisResult);
            var gitClient = _clientResolver.GetClient(analysisResult);

            // Get the target branch from the PR
            var pullRequest = await gitClient.GetPullRequestAsync(projectName, repositoryName, pullRequestId)
                .ConfigureAwait(false);

            // Extract branch name from refs/heads/branchname format
            var targetBranch = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty);

            return await gitClient.RetrieveCustomInstructionsAsync(
                projectName,
                repositoryName,
                targetBranch)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't fail - custom instructions are optional
            // The orchestrator can handle null gracefully
            System.Diagnostics.Debug.WriteLine($"Failed to retrieve custom instructions: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Adds a general comment to the pull request (not tied to a specific file or line).
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <param name="comment">The comment text (supports Markdown).</param>
    /// <returns>The created comment thread.</returns>
    public async Task<GitPullRequestCommentThread> AddCommentAsync(
        AnalysisResult analysisResult,
        string comment)
    {
        ValidateAnalysisResult(analysisResult);

        var (projectName, repositoryName, pullRequestId) = ExtractPullRequestInfo(analysisResult);
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.CreateCommentAsync(
            projectName,
            repositoryName,
            pullRequestId,
            comment)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Adds an inline code suggestion to a specific file and line in the pull request.
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <param name="suggestedCode">The suggested code change.</param>
    /// <param name="context">Additional context or explanation for the suggestion.</param>
    /// <param name="filePath">The file path for the inline comment.</param>
    /// <param name="lineFrom">The starting line number for the inline comment.</param>
    /// <param name="lineTo">Optional: The ending line number. If null, defaults to lineFrom.</param>
    /// <returns>The created comment thread.</returns>
    public async Task<GitPullRequestCommentThread> AddInlineSuggestionAsync(
        AnalysisResult analysisResult,
        string suggestedCode,
        string context,
        string filePath,
        int lineFrom,
        int? lineTo = null)
    {
        ValidateAnalysisResult(analysisResult);

        var (projectName, repositoryName, pullRequestId) = ExtractPullRequestInfo(analysisResult);
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.CreateCodeChangeCommentAsync(
            projectName,
            repositoryName,
            pullRequestId,
            suggestedCode,
            context,
            filePath,
            lineFrom,
            lineTo)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Appends text to the pull request description.
    /// Useful for adding AI analysis summaries or automated reports.
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <param name="textToAppend">The text to append (supports Markdown).</param>
    /// <param name="separator">Optional separator between existing description and new text.</param>
    /// <returns>The updated pull request.</returns>
    public async Task<GitPullRequest> AppendToDescriptionAsync(
        AnalysisResult analysisResult,
        string textToAppend,
        string separator = "\n\n---\n\n")
    {
        ValidateAnalysisResult(analysisResult);

        var (projectName, repositoryName, pullRequestId) = ExtractPullRequestInfo(analysisResult);
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.AppendToDescriptionAsync(
            projectName,
            repositoryName,
            pullRequestId,
            textToAppend,
            separator)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the analysis result has valid PR information.
    /// </summary>
    private static void ValidateAnalysisResult(AnalysisResult analysisResult)
    {
        if (analysisResult.GitInfo is null)
        {
            throw new InvalidOperationException("GitInfo is missing from the analysis result.");
        }

        if (analysisResult.GitInfo.Type != EGitInfoType.PullRequest)
        {
            throw new InvalidOperationException(
                $"Analysis result is not for a pull request. Type: {analysisResult.GitInfo.Type}");
        }
    }

    /// <summary>
    /// Extracts pull request information from the analysis result.
    /// </summary>
    private static (string projectName, string repositoryName, int pullRequestId) ExtractPullRequestInfo(
        AnalysisResult analysisResult)
    {
        var gitInfo = analysisResult.GitInfo!;

        if (!int.TryParse(gitInfo.Identifier, out var pullRequestId))
        {
            throw new InvalidOperationException(
                $"Invalid pull request identifier: {gitInfo.Identifier}");
        }

        var projectName = gitInfo.ProjectName
            ?? throw new InvalidOperationException("Project name is missing from GitInfo");

        return (projectName, gitInfo.RepositoryName, pullRequestId);
    }

}
