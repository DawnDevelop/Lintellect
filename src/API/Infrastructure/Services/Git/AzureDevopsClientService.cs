using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Infrastructure.Extensions;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Account.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text;

namespace devops_pr_analyzer.Infrastructure.Services.Git;

/// <summary>
/// Provides functionality for connecting to an Azure DevOps organization using a personal access token (PAT).
/// </summary>
/// <remarks>Use this service to establish authenticated connections to Azure DevOps REST APIs. The connection is
/// initialized with the provided organization URL and PAT, enabling access to organization-level resources.</remarks>
/// <param name="devopsPat">The personal access token used to authenticate requests to Azure DevOps. Cannot be null or empty.</param>
/// <param name="orgUri">The base URL of the Azure DevOps organization. (https://dev.azure.com/orgname) </param>
public class AzureDevopsClientService(string devopsPat, Uri orgUri) : IGitClient
{
    private readonly VssConnection _connection = new(orgUri, new VssOAuthAccessTokenCredential(devopsPat));

    public Task<GitHttpClient> GetHttpGitClient()
        => _connection.GetClientAsync<GitHttpClient>();

    public Task<ProjectHttpClient> GetProjectGitClient()
        => _connection.GetClientAsync<ProjectHttpClient>();

    public Task<IdentityHttpClient> GetIdentityGitClient()
        => _connection.GetClientAsync<IdentityHttpClient>();

    /// <inheritdoc />
    public async Task<GitPullRequest> GetPullRequestAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetHttpGitClient();
        return await gitClient.GetPullRequestAsync(projectName, repositoryName, pullRequestId);
    }

    /// <inheritdoc />
    public async Task<string?> GetFileAsync(
        string projectName,
        string repositoryName,
        string? branchName = null,
        params string[] possiblePaths)
    {
        var gitClient = await GetHttpGitClient();

        // If no branch specified, get the default branch
        var repository = await gitClient.GetRepositoryAsync(projectName, repositoryName);
        branchName ??= repository.DefaultBranch?.Replace("refs/heads/", string.Empty) ?? "main";

        foreach (var path in possiblePaths)
        {
            try
            {
                var content = await GetFileTextAsync(
                    projectName,
                    repositoryName,
                    path,
                    branchName,
                    useCommitId: false);

                if (content is not null)
                {
                    return content;
                }
            }
            catch
            {
                // File doesn't exist at this path, try next location
                continue;
            }
        }

        // File not found in any common location
        return null;
    }

    /// <summary>
    /// Retrieves the commits associated with a pull request.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <returns>A collection of commits in the pull request.</returns>
    public async Task<IReadOnlyList<GitCommitRef>> GetPullRequestCommitsAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetHttpGitClient();
        var commits = await gitClient.GetPullRequestCommitsAsync(projectName, repositoryName, pullRequestId);
        return commits;
    }

    /// <summary>
    /// Retrieves the file changes (diff) for a specific commit.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="commitId">The commit SHA to get changes for.</param>
    /// <returns>A collection of file changes in the commit.</returns>
    public async Task<GitCommitChanges> GetCommitChangesAsync(string projectName, string repositoryName, string commitId)
    {
        var gitClient = await GetHttpGitClient();
        return await gitClient.GetChangesAsync(projectName, commitId, repositoryName);
    }

    /// <summary>
    /// Retrieves the diff between two commits (typically used for pull request diffs).
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="baseCommitId">The base commit SHA (target branch).</param>
    /// <param name="targetCommitId">The target commit SHA (source branch/PR head).</param>
    /// <returns>A collection of file changes between the two commits.</returns>
    public async Task<GitCommitDiffs> GetCommitDiffsAsync(string projectName, string repositoryName, string baseCommitId, string targetCommitId)
    {
        var gitClient = await GetHttpGitClient();
        return await gitClient.GetCommitDiffsAsync(projectName, repositoryName, true, top: 1000,
            baseVersionDescriptor: new GitBaseVersionDescriptor { Version = baseCommitId, VersionType = GitVersionType.Commit },
            targetVersionDescriptor: new GitTargetVersionDescriptor { Version = targetCommitId, VersionType = GitVersionType.Commit });
    }

    /// <summary>
    /// Retrieves all file changes in a pull request by comparing the source and target branches.
    /// This is the primary method to get PR diffs for AI analysis.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <returns>A collection of file changes in the pull request.</returns>
    public async Task<GitCommitDiffs> GetPullRequestDiffAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetHttpGitClient();
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId);

        // Get diff between source and target branches
        return await gitClient.GetCommitDiffsAsync(
            projectName,
            repositoryName,
            true,
            top: 1000,
            baseVersionDescriptor: new GitBaseVersionDescriptor
            {
                Version = pullRequest.TargetRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            },
            targetVersionDescriptor: new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            });
    }

    /// <summary>
    /// Retrieves the file content at a specific commit.
    /// Useful for getting the full file context when analyzing specific changes.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="filePath">The path to the file in the repository.</param>
    /// <param name="commitId">The commit SHA to retrieve the file from.</param>
    /// <returns>A stream containing the file contents.</returns>
    public async Task<Stream> GetFileContentAsync(string projectName, string repositoryName, string filePath, string commitId)
    {
        var gitClient = await GetHttpGitClient();
        return await gitClient.GetItemContentAsync(
            project: projectName,
            repositoryId: repositoryName,
            path: filePath,
            versionDescriptor: new GitVersionDescriptor
            {
                Version = commitId,
                VersionType = GitVersionType.Commit
            })
            ;
    }

    /// <summary>
    /// Retrieves the actual text content of a file as a string.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="filePath">The path to the file in the repository.</param>
    /// <param name="versionIdentifier">The commit SHA or branch name to retrieve the file from.</param>
    /// <param name="useCommitId">True to use commit ID, false to use branch name.</param>
    /// <returns>The file content as a string, or null if the file doesn't exist.</returns>
    private async Task<string?> GetFileTextAsync(
        string projectName,
        string repositoryName,
        string filePath,
        string versionIdentifier,
        bool useCommitId = true)
    {
        try
        {
            var gitClient = await GetHttpGitClient();
            using var stream = await gitClient.GetItemContentAsync(
                project: projectName,
                repositoryId: repositoryName,
                path: filePath,
                versionDescriptor: new GitVersionDescriptor
                {
                    Version = versionIdentifier,
                    VersionType = useCommitId ? GitVersionType.Commit : GitVersionType.Branch
                })
                ;

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            // File might not exist in this commit (e.g., newly added or deleted file)
            return null;
        }
    }

    /// <summary>
    /// Retrieves the actual text content of a file as a string from a commit.
    /// </summary>
    public async Task<string?> GetFileTextAsync(string projectName, string repositoryName, string filePath, string commitId)
        => await GetFileTextAsync(projectName, repositoryName, filePath, commitId, useCommitId: true);

    /// <summary>
    /// Retrieves the unified diff format for a specific file change in a pull request.
    /// This provides the actual line-by-line changes suitable for AI analysis.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="filePath">The path to the changed file.</param>
    /// <param name="baseCommitId">The base commit SHA (before changes).</param>
    /// <param name="targetCommitId">The target commit SHA (after changes).</param>
    /// <returns>A unified diff string showing the changes, or null if unable to generate.</returns>
    public async Task<string?> GetFileDiffAsync(string projectName, string repositoryName, string filePath, string baseCommitId, string targetCommitId)
    {
        var baseContent = await GetFileTextAsync(projectName, repositoryName, filePath, baseCommitId);
        var targetContent = await GetFileTextAsync(projectName, repositoryName, filePath, targetCommitId);

        return DiffGenerationHelper.GenerateUnifiedDiff(filePath, baseContent, targetContent);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetPullRequestFileDiffsAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetHttpGitClient();
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId);

        // Get the list of changed files
        var commitDiffs = await gitClient.GetCommitDiffsAsync(
            projectName,
            repositoryName,
            true,
            top: 1000,
            baseVersionDescriptor: new GitBaseVersionDescriptor
            {
                Version = pullRequest.TargetRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            },
            targetVersionDescriptor: new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            })
            ;

        var fileDiffs = new Dictionary<string, string>();

        if (commitDiffs.Changes is null)
        {
            return fileDiffs;
        }

        // Get actual diff content for each changed file
        foreach (var change in commitDiffs.Changes)
        {
            if (change.Item?.Path is null)
                continue;

            var filePath = change.Item.Path;

            // Get the base and target commit IDs
            var baseCommitId = commitDiffs.BaseCommit ?? commitDiffs.CommonCommit;
            var targetCommitId = commitDiffs.TargetCommit;

            if (baseCommitId is null || targetCommitId is null)
                continue;

            var diff = await GetFileDiffAsync(projectName, repositoryName, filePath, baseCommitId, targetCommitId);

            if (diff is not null)
            {
                fileDiffs[filePath] = diff;
            }
        }

        return fileDiffs;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetPullRequestCompactDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        int contextLines = 3,
        int maxNewFileLines = 50,
        int maxLinesPerFile = 1000)
    {
        var gitClient = await GetHttpGitClient();
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId);

        // Get the list of changed files
        var commitDiffs = await gitClient.GetCommitDiffsAsync(
            projectName,
            repositoryName,
            true,
            top: 1000,
            baseVersionDescriptor: new GitBaseVersionDescriptor
            {
                Version = pullRequest.TargetRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            },
            targetVersionDescriptor: new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            })
            ;

        var compactDiffs = new Dictionary<string, string>();
        commitDiffs.Changes = commitDiffs.Changes.Where(x => !x.Item.IsFolder);

        if (commitDiffs.Changes is null)
        {
            return compactDiffs;
        }

        var baseCommitId = commitDiffs.BaseCommit ?? commitDiffs.CommonCommit;
        var targetCommitId = commitDiffs.TargetCommit;

        if (baseCommitId is null || targetCommitId is null)
            return compactDiffs;

        // Get compact diffs for each changed file
        foreach (var change in commitDiffs.Changes)
        {
            if (change.Item?.Path is null)
                continue;

            var filePath = change.Item.Path;
            var compactDiff = await GetFileCompactDiffAsync(
                projectName,
                repositoryName,
                filePath,
                baseCommitId,
                targetCommitId,
                contextLines,
                maxNewFileLines,
                maxLinesPerFile)
                ;

            if (compactDiff is not null)
            {
                compactDiffs[filePath] = compactDiff;
            }
        }

        return compactDiffs;
    }

    /// <summary>
    /// Gets a compact diff for a single file showing only changed hunks with context.
    /// </summary>
    private async Task<string?> GetFileCompactDiffAsync(
        string projectName,
        string repositoryName,
        string filePath,
        string baseCommitId,
        string targetCommitId,
        int contextLines,
        int maxNewFileLines,
        int maxLinesPerFile)
    {
        var baseContent = await GetFileTextAsync(projectName, repositoryName, filePath, baseCommitId);
        var targetContent = await GetFileTextAsync(projectName, repositoryName, filePath, targetCommitId);

        return DiffGenerationHelper.GenerateCompactDiff(filePath, baseContent, targetContent, contextLines, maxNewFileLines, maxLinesPerFile);
    }

    /// <inheritdoc />
    public async Task<GitPullRequestCommentThread> CreateCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string comment)
    {
        var gitClient = await GetHttpGitClient();

        var thread = new GitPullRequestCommentThread
        {
            Comments =
            [
                new Comment
                {
                    Content = comment,
                    CommentType = CommentType.Text
                }
            ],
            Status = CommentThreadStatus.Active
        };

        return await gitClient.CreateThreadAsync(
            thread,
            projectName,
            repositoryName,
            pullRequestId
            )
            ;
    }

    public async Task<GitPullRequestCommentThread> CreateCodeChangeCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string codeChange,
        string? context = null,
        string? filePath = null,
        int? lineFrom = null,
        int? lineTo = null)
    {
        var gitClient = await GetHttpGitClient();

        var commentContent = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context))
        {
            commentContent.AppendLine(context);
            commentContent.AppendLine();
        }

        commentContent.AppendLine("```suggestion");
        commentContent.AppendLine(codeChange);
        commentContent.Append("```");

        var thread = new GitPullRequestCommentThread
        {
            Comments =
            [
                new Comment
                {
                    Content = commentContent.ToString(),
                    CommentType = CommentType.CodeChange
                }
            ],
            Status = CommentThreadStatus.Active
        };

        // If filePath and line are provided, create an inline comment
        if (!string.IsNullOrWhiteSpace(filePath) && lineFrom.HasValue)
        {
            // For Azure DevOps suggestions to work correctly:
            // - RightFileStart.Line: The first line to be replaced
            // - RightFileStart.Offset: Character position on the start line (1-based)
            // - RightFileEnd.Line: The last line to be replaced
            // - RightFileEnd.Offset: Character position after the last character to replace
            //
            // To replace entire line(s), we set Offset to 1 (start of line) 
            // and RightFileEnd.Offset to a very large number to capture the whole line

            var endLine = lineTo ?? lineFrom.Value;

            thread.ThreadContext = new CommentThreadContext
            {
                FilePath = filePath,
                RightFileStart = new CommentPosition
                {
                    Line = lineFrom.Value,
                    Offset = 1  // Start at the beginning of the line
                },
                RightFileEnd = new CommentPosition
                {
                    Line = endLine,
                    Offset = int.MaxValue  // Extend to end of line (Azure DevOps will cap this automatically)
                }
            };
        }

        return await gitClient.CreateThreadAsync(
            thread,
            projectName,
            repositoryName,
            pullRequestId)
            ;
    }

    /// <inheritdoc />
    public async Task<GitPullRequest> AppendToDescriptionAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string textToAppend,
        string separator = "\n\n---\n\n")
    {
        var gitClient = await GetHttpGitClient();
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId)
            ;

        var existingDescription = pullRequest.Description ?? string.Empty;
        var newDescription = string.IsNullOrWhiteSpace(existingDescription)
            ? textToAppend
            : $"{existingDescription}{separator}{textToAppend}";

        return await UpdateDescriptionAsync(projectName, repositoryName, pullRequestId, newDescription)
            ;
    }

    /// <summary>
    /// Updates the entire pull request description.
    /// Warning: This replaces the existing description completely.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <param name="newDescription">The new description text (supports Markdown).</param>
    /// <returns>The updated pull request.</returns>
    private async Task<GitPullRequest> UpdateDescriptionAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string newDescription)
    {
        var gitClient = await GetHttpGitClient();

        var updatedPr = new GitPullRequest
        {
            Description = newDescription
        };

        return await gitClient.UpdatePullRequestAsync(
            updatedPr,
            projectName,
            repositoryName,
            pullRequestId);
    }


    // <inheritDoc />
    public async Task AddCodeOwnersToPr(
        string projectName,
        int pullRequestId,
        List<string> reviewer,
        string? repositoryName = null)
    {
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName!, pullRequestId);

        var ids = await GetUserIdsFromEmailsAsync(reviewer);

        if (ids.Count == 0)
        {
            // No valid users found, skip adding reviewers
            return;
        }

        // Get existing reviewers to avoid duplicates
        var existingReviewerIds = pullRequest.Reviewers?.Select(r => r.Id).ToHashSet() ?? [];

        var reviewersToAdd = ids
            .Where(id => !existingReviewerIds.Contains(id.ToString()))
            .Select(x => new IdentityRefWithVote
            {
                
                Id = x.ToString(),
                IsRequired = true
            })
            .ToList();

        if (reviewersToAdd.Count > 0)
        {

            var gitClient = await GetHttpGitClient();

            var result = await gitClient.CreatePullRequestReviewersAsync(
                [.. reviewersToAdd], 
                projectName, 
                repositoryName, 
                pullRequestId);
        }
    }

    private async Task<List<Guid>> GetUserIdsFromEmailsAsync(List<string> emails)
    {
        var projectClient = await GetIdentityGitClient();
        var identities = await projectClient.ReadIdentitiesAsync(IdentitySearchFilter.General, string.Join(",", emails));
        return [.. identities.Select(x => x.Id)];
    }

}
