using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace devops_pr_analyzer.Services;

/// <summary>
/// Provides functionality for connecting to an Azure DevOps organization using a personal access token (PAT).
/// </summary>
/// <remarks>Use this service to establish authenticated connections to Azure DevOps REST APIs. The connection is
/// initialized with the provided organization URL and PAT, enabling access to organization-level resources.</remarks>
/// <param name="devopsPat">The personal access token used to authenticate requests to Azure DevOps. Cannot be null or empty.</param>
/// <param name="orgUri">The base URL of the Azure DevOps organization. (https://dev.azure.com/orgname) </param>
public class AzureDevopsClientService(string devopsPat, Uri orgUri)
{
    private readonly VssConnection _connection = new(orgUri, new VssOAuthAccessTokenCredential(devopsPat));

    public Task<GitHttpClient> GetGitClient()
        => _connection.GetClientAsync<GitHttpClient>();

    public Task<ProjectHttpClient> GetProjectClient()
        => _connection.GetClientAsync<ProjectHttpClient>();

    /// <summary>
    /// Retrieves the pull request details for the specified pull request ID.
    /// </summary>
    /// <param name="projectName">The name or ID of the Azure DevOps project.</param>
    /// <param name="repositoryName">The name or ID of the repository.</param>
    /// <param name="pullRequestId">The ID of the pull request.</param>
    /// <returns>The pull request object containing metadata and status.</returns>
    public async Task<GitPullRequest> GetPullRequestAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetGitClient().ConfigureAwait(false);
        return await gitClient.GetPullRequestAsync(projectName, repositoryName, pullRequestId).ConfigureAwait(false);
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
        var gitClient = await GetGitClient().ConfigureAwait(false);
        var commits = await gitClient.GetPullRequestCommitsAsync(projectName, repositoryName, pullRequestId).ConfigureAwait(false);
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
        var gitClient = await GetGitClient().ConfigureAwait(false);
        return await gitClient.GetChangesAsync(projectName, commitId, repositoryName).ConfigureAwait(false);
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
        var gitClient = await GetGitClient().ConfigureAwait(false);
        return await gitClient.GetCommitDiffsAsync(projectName, repositoryName, true, top: 1000,
            baseVersionDescriptor: new GitBaseVersionDescriptor { Version = baseCommitId, VersionType = GitVersionType.Commit },
            targetVersionDescriptor: new GitTargetVersionDescriptor { Version = targetCommitId, VersionType = GitVersionType.Commit })
            .ConfigureAwait(false);
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
        var gitClient = await GetGitClient().ConfigureAwait(false);
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId).ConfigureAwait(false);

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
            })
            .ConfigureAwait(false);
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
        var gitClient = await GetGitClient().ConfigureAwait(false);
        return await gitClient.GetItemContentAsync(
            project: projectName,
            repositoryId: repositoryName,
            path: filePath,
            versionDescriptor: new GitVersionDescriptor
            {
                Version = commitId,
                VersionType = GitVersionType.Commit
            })
            .ConfigureAwait(false);
    }


}
