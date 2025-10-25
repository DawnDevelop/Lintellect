using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.shared.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace devops_pr_analyzer.Infrastructure.Services.Git;

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
        AnalysisRequest analysisResult,
        int contextLines = 3,
        int maxNewFileLines = 50,
        int maxLinesPerFile = 1000)
    {
        // Get the appropriate client based on the provider
        var gitClient = _clientResolver.GetClient(analysisResult);

        var diffs = await gitClient.GetPullRequestCompactDiffsAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId,
            contextLines,
            maxNewFileLines,
            maxLinesPerFile);

        return diffs;
    }

    /// <summary>
    /// Retrieves custom project-specific instructions from copilot-instructions.md file.
    /// Searches common locations in the target branch of the pull request.
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <returns>Custom instructions content, or null if not found.</returns>
    public async Task<string?> GetCustomInstructionsAsync(
        AnalysisRequest analysisResult)
    {

        var gitClient = _clientResolver.GetClient(analysisResult);

        // Get the target branch from the PR
        var pullRequest = await gitClient.GetPullRequestAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId);
        // Extract branch name from refs/heads/branchname format
        var targetBranch = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty);

        // Common locations where copilot-instructions.md might be stored
        var possiblePaths = new[]
        {
            "/.github/copilot-instructions.md",
            "/.github/COPILOT-INSTRUCTIONS.md",
            "/.copilot/copilot-instructions.md",
            "/docs/copilot-instructions.md",
            "/copilot-instructions.md",
            "/COPILOT-INSTRUCTIONS.md"
        };

        return await gitClient.GetFileAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            targetBranch,
            possiblePaths);
    }

    /// <summary>
    /// Retrieves the contents of the CODEOWNERS file from the target branch of the pull request specified in the
    /// analysis result.
    /// </summary>
    /// <remarks>The method searches for the CODEOWNERS file in several common locations within the
    /// repository's target branch. If multiple files are present, the first match in the search order is
    /// returned.</remarks>
    /// <param name="analysisResult">The analysis result containing information about the pull request and repository from which to locate the
    /// CODEOWNERS file. Cannot be null.</param>
    /// <returns>A string containing the contents of the CODEOWNERS file if found; otherwise, null.</returns>
    public async Task<string?> GetCodeOwnersFileAsync(AnalysisRequest analysisResult)
    {
        var gitClient = _clientResolver.GetClient(analysisResult);


        // Get the target branch from the PR
        var pullRequest = await gitClient.GetPullRequestAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId);
        
        // Extract branch name from refs/heads/branchname format
        var targetBranch = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty);

        // Common locations where copilot-instructions.md might be stored
        var possiblePaths = new[]
        {
            "/.github/CODEOWNERS.md",
            "/.github/CODEOWNERS",
            "/CODEOWNERS.md",
            "/CODEOWNERS",
            "/.docs/CODEOWNERS.md",
            "/docs/CODEOWNERS.md",
            "/.docs/CODEOWNERS",
            "/docs/CODEOWNERS"
        };

        var content = await gitClient.GetFileAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            targetBranch,
            possiblePaths);

        return content;
    }

    /// <summary>
    /// Adds a general comment to the pull request (not tied to a specific file or line).
    /// </summary>
    /// <param name="analysisResult">Analysis result containing PR information.</param>
    /// <param name="comment">The comment text (supports Markdown).</param>
    /// <returns>The created comment thread.</returns>
    public async Task<GitPullRequestCommentThread> AddCommentAsync(
        AnalysisRequest analysisResult,
        string comment)
    {
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.CreateCommentAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId,
            comment);
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
        AnalysisRequest analysisResult,
        string suggestedCode,
        string context,
        string filePath,
        int lineFrom,
        int? lineTo = null)
    {
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.CreateCodeChangeCommentAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId,
            suggestedCode,
            context,
            filePath,
            lineFrom,
            lineTo);
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
        AnalysisRequest analysisResult,
        string textToAppend,
        string separator = "\n\n---\n\n")
    {
        var gitClient = _clientResolver.GetClient(analysisResult);

        return await gitClient.AppendToDescriptionAsync(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo!.RepositoryName,
            analysisResult.GitInfo!.PullRequestId,
            textToAppend,
            separator);
    }

    /// <summary>
    /// Adds the specified users as code owners to the pull request identified in the analysis result.
    /// </summary>
    /// <param name="analysisResult">An object containing information about the pull request and repository to which code owners will be added. Must
    /// not be null and must contain valid Git information.</param>
    /// <param name="userEmails">A list of email addresses representing the users to be added as code owners to the pull request. Each email must
    /// correspond to a valid user in the repository.</param>
    public async Task AddCodeOwnersToPullRequest(
        AnalysisRequest analysisResult,
        List<string> userEmails)
    {
        var gitClient = _clientResolver.GetClient(analysisResult);

        await gitClient.AddCodeOwnersToPr(
            analysisResult.GitInfo!.ProjectName!,
            analysisResult.GitInfo.PullRequestId,
            userEmails,
            analysisResult.GitInfo.RepositoryName
        );
    }

}
