using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Application.Models;
using devops_pr_analyzer.Infrastructure.Extensions;
using devops_pr_analyzer.shared.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Account.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.Security.Client;
using Microsoft.VisualStudio.Services.Tokens.TokenAdmin.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.HttpClients;
using Octokit;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace devops_pr_analyzer.Infrastructure.Services.Git;

/// <summary>
/// Provides functionality for connecting to an Azure DevOps organization using a personal access token (PAT).
/// </summary>
/// <remarks>Use this service to establish authenticated connections to Azure DevOps REST APIs. The connection is
/// initialized with the provided organization URL and PAT, enabling access to organization-level resources.</remarks>
/// <param name="devopsPat">The personal access token used to authenticate requests to Azure DevOps. Cannot be null or empty.</param>
/// <param name="orgUri">The base URL of the Azure DevOps organization. (https://dev.azure.com/orgname) </param>
public class AzureDevopsClientService : IGitClient
{
    private readonly VssConnection _connection;

    // Thread-safe lazy initialization using Lazy<Task<T>>
    private readonly Lazy<Task<GitHttpClient>> _gitClient;
    private readonly Lazy<Task<ProjectHttpClient>> _projectClient;
    private readonly Lazy<Task<IdentityHttpClient>> _identityClient;
    private readonly Lazy<Task<SecurityHttpClient>> _securityClient;

    public AzureDevopsClientService(string devopsPat, Uri orgUri)
    {
        _connection = new VssConnection(orgUri, new VssOAuthAccessTokenCredential(devopsPat));

        // Initialize lazy clients with proper async factory
        _gitClient = new Lazy<Task<GitHttpClient>>(() => _connection.GetClientAsync<GitHttpClient>());
        _projectClient = new Lazy<Task<ProjectHttpClient>>(() => _connection.GetClientAsync<ProjectHttpClient>());
        _identityClient = new Lazy<Task<IdentityHttpClient>>(() => _connection.GetClientAsync<IdentityHttpClient>());
        _securityClient = new Lazy<Task<SecurityHttpClient>>(() => _connection.GetClientAsync<SecurityHttpClient>());
    }

    public Task<GitHttpClient> GetHttpGitClient() => _gitClient.Value;

    public Task<ProjectHttpClient> GetProjectGitClient() => _projectClient.Value;

    public Task<IdentityHttpClient> GetIdentityGitClient() => _identityClient.Value;

    public Task<SecurityHttpClient> GetSecurityClient() => _securityClient.Value;

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
    public async Task<List<CheckPermissionResult>> HasSufficientPermissionsAsync(AnalysisRequest analysisRequest)
    {
        var project = analysisRequest.GitInfo!.ProjectName;
        var repoName = analysisRequest.GitInfo.RepositoryName;
        var pullRequestId = analysisRequest.GitInfo.PullRequestId;
        var results = new List<CheckPermissionResult>();

        try
        {
            // Test basic repository access
            var gitClient = await GetHttpGitClient();
            var repositories = await gitClient.GetRepositoriesAsync(project);

            // Check if the specific repository exists
            var targetRepo = repositories.FirstOrDefault(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));
            if (targetRepo == null)
            {
                results.Add(new CheckPermissionResult(false, $"Repository '{repoName}' not found in project '{project}'"));
                return results;
            }

            // Test pull request access
            var pullRequest = await gitClient.GetPullRequestAsync(project, repoName, pullRequestId);
            if (pullRequest == null)
            {
                results.Add(new CheckPermissionResult(false, $"Pull request #{pullRequestId} not found in repository '{repoName}'"));
                return results;
            }

            // Test required permissions based on enabled features
            // Code read permission (always required)
            var codeReadResult = await TestPermissionAsync(async () =>
            {
                await gitClient.GetRepositoriesAsync(project);
            });
            results.Add(new CheckPermissionResult(codeReadResult.Success, codeReadResult.Success ? null : $"Code Read: {codeReadResult.Reason}"));

            // Code write permission (required for inline suggestions)
            if (analysisRequest.EnableInlineSuggestions)
            {
                var codeWriteResult = await TestPermissionAsync(async () =>
                {
                    // Test by attempting to create a push (will fail validation but tests write scope)
                    await gitClient.CreatePushAsync(new GitPush(), project, repoName);
                });
                results.Add(new CheckPermissionResult(codeWriteResult.Success, codeWriteResult.Success ? null : $"Code Write: {codeWriteResult.Reason}"));
            }

            // Pull request comment permission (required for summary comments and inline suggestions)
            if (analysisRequest.EnableSummaryComment || analysisRequest.EnableInlineSuggestions)
            {
                var prCommentResult = await TestPermissionAsync(async () =>
                {
                    // Test by attempting to create a comment thread (will fail validation but tests comment scope)
                    var badThread = new GitPullRequestCommentThread();
                    await gitClient.CreateThreadAsync(badThread, project, repoName, pullRequestId);
                });
                results.Add(new CheckPermissionResult(prCommentResult.Success, prCommentResult.Success ? null : $"Pull Request Comments: {prCommentResult.Reason}"));
            }

            // Pull request edit permission (required for description updates)
            if (analysisRequest.EnableDescriptionSummary)
            {
                var prEditResult = await TestPermissionAsync(async () =>
                {
                    // Test by attempting to update PR (will fail validation but tests edit scope)
                    var invalidUpdate = new GitPullRequest { Title = "" };
                    await gitClient.UpdatePullRequestAsync(invalidUpdate, project, repoName, pullRequestId);
                });
                results.Add(new CheckPermissionResult(prEditResult.Success, prEditResult.Success ? null : $"Pull Request Edit: {prEditResult.Reason}"));
            }

            // Identity read permission (required for code owners)
            if (analysisRequest.EnableAzureDevopsCodeOwners)
            {
                var identityReadResult = await TestPermissionAsync(async () =>
                {
                    var identityClient = await GetIdentityGitClient();
                    var ids = await identityClient.ReadIdentitiesAsync(IdentitySearchFilter.General, "me");
                    _ = ids.Count;
                });
                results.Add(new CheckPermissionResult(identityReadResult.Success, identityReadResult.Success ? null : $"Identity Read: {identityReadResult.Reason}"));
            }

            return results;
        }
        catch (VssUnauthorizedException)
        {
            results.Add(new CheckPermissionResult(false, "Authentication failed: Invalid or expired PAT token"));
            return results;
        }
        catch (Exception ex)
        {
            results.Add(new CheckPermissionResult(false, $"Permission check failed: {ex.Message}"));
            return results;
        }
    }

    private static async Task<(bool Success, string? Reason)> TestPermissionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (VssUnauthorizedException)
        {
            return (false, "Insufficient permissions - PAT token lacks required scope");
        }
        catch (VssServiceException)
        {
            return (true, null); //Because of forced 400
        }
        catch (Exception ex)
        {
            return (false, $"Permission test failed: {ex.Message}");
        }
    }

    // <inheritDoc />
    public async Task AddCodeOwnersToPr(
        string projectName,
        int pullRequestId,
        CodeOwnersResult codeOwners,
        string? repositoryName = null)
    {
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName!, pullRequestId);

        var resolvedCodeOwners = await ResolveCodeOwnersAsync(codeOwners.CodeOwners);

        if (resolvedCodeOwners.Count == 0)
        {
            // No valid users found, skip adding reviewers
            return;
        }

        // Get existing reviewers to avoid duplicates
        var existingReviewerIds = pullRequest.Reviewers?.Select(r => r.Id).ToHashSet() ?? [];

        var reviewersToAdd = resolvedCodeOwners
            .Where(x => !string.IsNullOrEmpty(x.AzureDevOpsId) && !existingReviewerIds.Contains(x.AzureDevOpsId))
            .Select(x => new IdentityRefWithVote
            {
                Id = x.AzureDevOpsId!,
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


    /// <summary>
    /// Resolves Azure DevOps identities (users and teams) from CODEOWNERS entries.
    /// </summary>
    /// <param name="projectName">The Azure DevOps project name.</param>
    /// <param name="codeOwners">List of code owner entries to resolve.</param>
    /// <returns>List of resolved identities with Azure DevOps IDs.</returns>
    public async Task<List<ResolvedCodeOwner>> ResolveCodeOwnersAsync(List<CodeOwner> codeOwners)
    {
        var identityClient = await GetIdentityGitClient();
        var resolvedOwners = new List<ResolvedCodeOwner>();

        foreach (var owner in codeOwners)
        {
            var resolvedOwner = new ResolvedCodeOwner
            {
                Name = owner.Name,
                Type = owner.Type,
                Email = owner.Email,
                DisplayName = owner.DisplayName
            };

            // Try to resolve the identity in Azure DevOps
            var identities = await identityClient.ReadIdentitiesAsync(
                IdentitySearchFilter.General,
                owner.Name);

            if (identities.Any())
            {
                var identity = identities.First();
                resolvedOwner.AzureDevOpsId = identity.Id.ToString();
                resolvedOwner.DisplayName = identity.DisplayName ?? identity.Id.ToString();

                // Determine if it's a team or user based on identity properties
                if (identity.IsContainer == true)
                {
                    resolvedOwner.Type = CodeOwnerType.Team;
                }
                else
                {
                    resolvedOwner.Type = CodeOwnerType.User;
                }
            }
            else
            {
                // If not found, try to resolve as email
                if (!string.IsNullOrEmpty(owner.Email))
                {
                    var emailIdentities = await identityClient.ReadIdentitiesAsync(
                        IdentitySearchFilter.General,
                        owner.Email);

                    if (emailIdentities.Any())
                    {
                        var identity = emailIdentities.First();
                        resolvedOwner.AzureDevOpsId = identity.Id.ToString();
                        resolvedOwner.DisplayName = identity.DisplayName ?? identity.Id.ToString();
                        resolvedOwner.Type = CodeOwnerType.User;
                    }
                }
            }

            resolvedOwners.Add(resolvedOwner);
        }

        return resolvedOwners;
    }
}

/// <summary>
/// Represents a resolved code owner with Azure DevOps identity information.
/// </summary>
public class ResolvedCodeOwner
{
    public string Name { get; set; } = string.Empty;
    public CodeOwnerType Type { get; set; }
    public string? Email { get; set; }
    public string? AzureDevOpsId { get; set; }
    public string? DisplayName { get; set; }
}
