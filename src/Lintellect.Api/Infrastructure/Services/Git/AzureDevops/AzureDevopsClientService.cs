using System.Text;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Api.Infrastructure.Extensions;
using Lintellect.Shared.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.Security.Client;
using Microsoft.VisualStudio.Services.WebApi;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using AdoWorkItemExpand = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemExpand;
using SharedWorkItemReference = Lintellect.Shared.Models.WorkItemReference;

namespace Lintellect.Api.Infrastructure.Services.Git.AzureDevops;

/// <summary>
/// Provides functionality for connecting to an Azure DevOps organization using a personal access token (PAT).
/// </summary>
/// <remarks>Use this service to establish authenticated connections to Azure DevOps REST APIs. The connection is
/// initialized with the provided organization URL and PAT, enabling access to organization-level resources.</remarks>
/// <param name="devopsPat">The personal access token used to authenticate requests to Azure DevOps. Cannot be null or empty.</param>
/// <param name="orgUri">The base URL of the Azure DevOps organization. (https://dev.azure.com/orgname) </param>
public class AzureDevopsClientService : IGitClient
{
    private static readonly string[] DefaultWorkItemBodyFields =
    [
        "System.Description",
        "Microsoft.VSTS.Common.AcceptanceCriteria",
        "Microsoft.VSTS.TCM.ReproSteps"
    ];

    private readonly VssConnection _connection;
    private readonly ILogger _logger;
    private readonly IReadOnlyList<string> _workItemBodyFields;

    // Thread-safe lazy initialization using Lazy<Task<T>>
    private readonly Lazy<Task<GitHttpClient>> _gitClient;
    private readonly Lazy<Task<ProjectHttpClient>> _projectClient;
    private readonly Lazy<Task<IdentityHttpClient>> _identityClient;
    private readonly Lazy<Task<SecurityHttpClient>> _securityClient;
    private readonly Lazy<Task<WorkItemTrackingHttpClient>> _witClient;

    public AzureDevopsClientService(string devopsPat, Uri orgUri, ILogger logger, IReadOnlyList<string>? workItemBodyFields = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workItemBodyFields = workItemBodyFields is { Count: > 0 } ? workItemBodyFields : DefaultWorkItemBodyFields;
        _connection = new VssConnection(orgUri, new VssOAuthAccessTokenCredential(devopsPat));

        // Initialize lazy clients with proper async factory
        _gitClient = new Lazy<Task<GitHttpClient>>(() => _connection.GetClientAsync<GitHttpClient>());
        _projectClient = new Lazy<Task<ProjectHttpClient>>(() => _connection.GetClientAsync<ProjectHttpClient>());
        _identityClient = new Lazy<Task<IdentityHttpClient>>(() => _connection.GetClientAsync<IdentityHttpClient>());
        _securityClient = new Lazy<Task<SecurityHttpClient>>(() => _connection.GetClientAsync<SecurityHttpClient>());
        _witClient = new Lazy<Task<WorkItemTrackingHttpClient>>(() => _connection.GetClientAsync<WorkItemTrackingHttpClient>());
    }

    /// <inheritdoc />
    public async Task<List<SharedWorkItemReference>> GetLinkedWorkItemsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        IReadOnlyList<SharedWorkItemReference>? hints = null)
    {
        var gitClient = await GetHttpGitClient();
        var refs = await gitClient.GetPullRequestWorkItemRefsAsync(projectName, repositoryName, pullRequestId);

        var ids = new List<int>();
        foreach (var r in refs ?? [])
        {
            if (int.TryParse(r.Id, out var id))
            {
                ids.Add(id);
            }
        }

        if (ids.Count == 0)
        {
            return [];
        }

        var witClient = await _witClient.Value;
        // No field restriction: requesting a field name that doesn't exist in the collection
        // (custom process templates) fails the whole call, whereas fetching everything lets
        // BuildBody pick whichever configured fields each work item actually has.
        var items = await witClient.GetWorkItemsAsync(ids, expand: AdoWorkItemExpand.None);

        return [.. items.Select(MapWorkItem)];
    }

    private SharedWorkItemReference MapWorkItem(AdoWorkItem item)
    {
        var fields = item.Fields ?? new Dictionary<string, object>();
        string? GetField(string key) => fields.TryGetValue(key, out var v) ? v?.ToString() : null;

        return new SharedWorkItemReference(
            Id: item.Id?.ToString() ?? string.Empty,
            Url: item.Url,
            Title: GetField("System.Title"),
            Body: BuildBody(GetField),
            State: GetField("System.State"),
            Type: GetField("System.WorkItemType"));
    }

    private string? BuildBody(Func<string, string?> getField)
    {
        var sections = _workItemBodyFields
            .Select(field => (Label: ToFieldLabel(field), Value: StripHtml(getField(field))))
            .Where(section => !string.IsNullOrWhiteSpace(section.Value))
            .Select(section => $"{section.Label}: {section.Value}")
            .ToList();

        return sections.Count > 0 ? string.Join("\n\n", sections) : null;
    }

    /// <summary>
    /// "Microsoft.VSTS.TCM.ReproSteps" → "Repro Steps".
    /// </summary>
    private static string ToFieldLabel(string fieldReferenceName)
    {
        var lastSegment = fieldReferenceName[(fieldReferenceName.LastIndexOf('.') + 1)..];
        return System.Text.RegularExpressions.Regex.Replace(lastSegment, "(?<=[a-z])(?=[A-Z])", " ");
    }

    private static string? StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var noTags = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", " ");
        var collapsed = System.Text.RegularExpressions.Regex.Replace(noTags, @"\s+", " ").Trim();
        return collapsed;
    }

    public Task<GitHttpClient> GetHttpGitClient()
    {
        return _gitClient.Value;
    }

    public Task<ProjectHttpClient> GetProjectGitClient()
    {
        return _projectClient.Value;
    }

    public Task<IdentityHttpClient> GetIdentityGitClient()
    {
        return _identityClient.Value;
    }

    public Task<SecurityHttpClient> GetSecurityClient()
    {
        return _securityClient.Value;
    }

    /// <inheritdoc />
    public async Task<PullRequest> GetPullRequestAsync(string projectName, string repositoryName, int pullRequestId)
    {
        var gitClient = await GetHttpGitClient();
        var azureDevOpsPr = await gitClient.GetPullRequestAsync(projectName, repositoryName, pullRequestId);
        return MapToGenericPullRequest(azureDevOpsPr);
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
                Version = pullRequest.TargetRefName?.Replace("refs/heads/", string.Empty) ?? string.Empty,
                VersionType = GitVersionType.Branch
            },
            targetVersionDescriptor: new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty) ?? string.Empty,
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
            });
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
                });

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
    private async Task<string?> GetFileTextAsync(string projectName, string repositoryName, string filePath, string commitId)
    {
        return await GetFileTextAsync(projectName, repositoryName, filePath, commitId, useCommitId: true);
    }

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
    private async Task<string?> GetFileDiffAsync(string projectName, string repositoryName, string filePath, string baseCommitId, string targetCommitId)
    {
        var baseContent = await GetFileTextAsync(projectName, repositoryName, filePath, baseCommitId);
        var targetContent = await GetFileTextAsync(projectName, repositoryName, filePath, targetCommitId);

        return DiffGenerationHelper.GenerateUnifiedDiff(baseContent, targetContent);
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
                Version = pullRequest.TargetRefName?.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            },
            targetVersionDescriptor: new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty),
                VersionType = GitVersionType.Branch
            });

        var fileDiffs = new Dictionary<string, string>();

        if (commitDiffs.Changes is null)
        {
            return fileDiffs;
        }

        // Get actual diff content for each changed file
        foreach (var change in commitDiffs.Changes)
        {
            if (change.Item?.Path is null)
            {
                continue;
            }

            var filePath = change.Item.Path;

            // Get the base and target commit IDs
            var baseCommitId = commitDiffs.BaseCommit ?? commitDiffs.CommonCommit;
            var targetCommitId = commitDiffs.TargetCommit;

            if (baseCommitId is null || targetCommitId is null)
            {
                continue;
            }

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
        int contextLines)
    {
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId);

        return await GetCompactDiffsCoreAsync(
            projectName,
            repositoryName,
            new GitBaseVersionDescriptor
            {
                Version = pullRequest.TargetRefName?.Replace("refs/heads/", string.Empty) ?? string.Empty,
                VersionType = GitVersionType.Branch
            },
            new GitTargetVersionDescriptor
            {
                Version = pullRequest.SourceRefName?.Replace("refs/heads/", string.Empty) ?? string.Empty,
                VersionType = GitVersionType.Branch
            },
            contextLines);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetCompactDiffsBetweenCommitsAsync(
        string projectName,
        string repositoryName,
        string baseCommitId,
        string targetCommitId,
        int contextLines)
    {
        return await GetCompactDiffsCoreAsync(
            projectName,
            repositoryName,
            new GitBaseVersionDescriptor
            {
                Version = baseCommitId,
                VersionType = GitVersionType.Commit
            },
            new GitTargetVersionDescriptor
            {
                Version = targetCommitId,
                VersionType = GitVersionType.Commit
            },
            contextLines);
    }

    private async Task<Dictionary<string, string>> GetCompactDiffsCoreAsync(
        string projectName,
        string repositoryName,
        GitBaseVersionDescriptor baseVersionDescriptor,
        GitTargetVersionDescriptor targetVersionDescriptor,
        int contextLines)
    {
        var gitClient = await GetHttpGitClient();

        // Get the list of changed files
        var commitDiffs = await gitClient.GetCommitDiffsAsync(
            projectName,
            repositoryName,
            true,
            top: 1000,
            baseVersionDescriptor: baseVersionDescriptor,
            targetVersionDescriptor: targetVersionDescriptor);

        var compactDiffs = new Dictionary<string, string>();
        commitDiffs.Changes = commitDiffs.Changes.Where(x => !x.Item.IsFolder);

        if (commitDiffs.Changes is null)
        {
            return compactDiffs;
        }

        var baseCommitId = commitDiffs.BaseCommit ?? commitDiffs.CommonCommit;
        var targetCommitId = commitDiffs.TargetCommit;

        if (baseCommitId is null || targetCommitId is null)
        {
            return compactDiffs;
        }

        // Get compact diffs for each changed file
        foreach (var change in commitDiffs.Changes)
        {
            if (change.Item?.Path is null)
            {
                continue;
            }

            var filePath = change.Item.Path;
            var baseContent = await GetFileTextAsync(projectName, repositoryName, filePath, baseCommitId);
            var targetContent = await GetFileTextAsync(projectName, repositoryName, filePath, targetCommitId);

            var diff = DiffGenerationHelper.GenerateUnifiedDiff(baseContent, targetContent, contextLines);

            if (diff is not null)
            {
                compactDiffs[filePath] = diff;
            }
        }

        return compactDiffs;
    }

    /// <inheritdoc />
    public async Task<PullRequestCommentThread> CreateCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string comment,
        int? threadId = null,
        bool isResolved = false)
    {
        var gitClient = await GetHttpGitClient();

        var threadStatus = isResolved
            ? Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Closed
            : Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Active;

        var thread = new GitPullRequestCommentThread
        {
            Comments =
            [
                new Comment
                {
                    Content = comment,
                    CommentType = Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.Text
                }
            ],
            Status = threadStatus
        };

        GitPullRequestCommentThread threadResult = threadId.HasValue
            ? await gitClient.UpdateThreadAsync(
                thread, projectName, repositoryName, pullRequestId, threadId.Value)
            : await gitClient.CreateThreadAsync(
                thread,
                projectName,
                repositoryName,
                pullRequestId);
        return MapToGenericCommentThread(threadResult);
    }

    public async Task<PullRequestCommentThread> CreateCodeChangeCommentAsync(
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
                    CommentType = Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.CodeChange
                }
            ],
            Status = Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Active
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

            thread.ThreadContext = new Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadContext
            {
                FilePath = filePath,
                RightFileStart = new Microsoft.TeamFoundation.SourceControl.WebApi.CommentPosition
                {
                    Line = lineFrom.Value,
                    Offset = 1  // Start at the beginning of the line
                },
                RightFileEnd = new Microsoft.TeamFoundation.SourceControl.WebApi.CommentPosition
                {
                    Line = endLine,
                    Offset = int.MaxValue  // Extend to end of line (Azure DevOps will cap this automatically)
                }
            };
        }

        var azureDevOpsThread = await gitClient.CreateThreadAsync(
            thread,
            projectName,
            repositoryName,
            pullRequestId)
            ;

        return MapToGenericCommentThread(azureDevOpsThread);
    }

    /// <inheritdoc />
    public async Task<PullRequest> AppendToDescriptionAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string textToAppend,
        string separator = "\n\n---\n\n")
    {
        var pullRequest = await GetPullRequestAsync(projectName, repositoryName, pullRequestId);

        var existingDescription = pullRequest.Description ?? string.Empty;
        var newDescription = string.IsNullOrWhiteSpace(existingDescription)
            ? textToAppend
            : $"{existingDescription}{separator}{textToAppend}";

        var updatedPr = await UpdateDescriptionAsync(projectName, repositoryName, pullRequestId, newDescription);
        return MapToGenericPullRequest(updatedPr);
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
            var (success, reason) = await TestPermissionAsync(async () => await gitClient.GetRepositoriesAsync(project));
            results.Add(new CheckPermissionResult(success, success ? null : $"Code Read: {reason}"));

            // Code write permission (required for inline suggestions)
            if (analysisRequest.EnableInlineSuggestions)
            {
                var (Success, Reason) = await TestPermissionAsync(async () =>
                    // Test by attempting to create a push (will fail validation but tests write scope)
                    await gitClient.CreatePushAsync(new GitPush(), project, repoName));
                results.Add(new CheckPermissionResult(Success, Success ? null : $"Code Write: {Reason}"));
            }

            // Pull request comment permission (required for summary comments and inline suggestions)
            if (analysisRequest.EnableSummaryComment || analysisRequest.EnableInlineSuggestions)
            {
                var (Success, Reason) = await TestPermissionAsync(async () =>
                {
                    // Test by attempting to create a comment thread (will fail validation but tests comment scope)
                    var badThread = new GitPullRequestCommentThread();
                    await gitClient.CreateThreadAsync(badThread, project, repoName, pullRequestId);
                });
                results.Add(new CheckPermissionResult(Success, Success ? null : $"Pull Request Comments: {Reason}"));
            }

            // Pull request edit permission (required for description updates)
            if (analysisRequest.EnableDescriptionSummary)
            {
                var (Success, Reason) = await TestPermissionAsync(async () =>
                {
                    // Test by attempting to update PR (will fail validation but tests edit scope)
                    var invalidUpdate = new GitPullRequest { Title = "" };
                    await gitClient.UpdatePullRequestAsync(invalidUpdate, project, repoName, pullRequestId);
                });
                results.Add(new CheckPermissionResult(Success, Success ? null : $"Pull Request Edit: {Reason}"));
            }

            // Work item read permission (required for work item context)
            if (analysisRequest.EnableWorkItemContext)
            {
                var (Success, Reason) = await TestPermissionAsync(async () =>
                    // Exercises the exact API path used by GetLinkedWorkItemsAsync — validates
                    // that the PAT has "Work Items (Read)" in addition to "Code (Read)".
                    _ = await gitClient.GetPullRequestWorkItemRefsAsync(project, repoName, pullRequestId));
                results.Add(new CheckPermissionResult(Success, Success ? null : $"Work Item Read: {Reason}"));
            }

            // Identity read permission (required for code owners)
            if (analysisRequest.EnableAzureDevopsCodeOwners)
            {
                var (Success, Reason) = await TestPermissionAsync(async () =>
                {
                    var identityClient = await GetIdentityGitClient();
                    var ids = await identityClient.ReadIdentitiesAsync(IdentitySearchFilter.General, "me");
                    _ = ids.Count;
                });
                results.Add(new CheckPermissionResult(Success, Success ? null : $"Identity Read: {Reason}"));
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
        var gitClient = await GetHttpGitClient();
        var azureDevOpsPr = await gitClient.GetPullRequestAsync(projectName, repositoryName!, pullRequestId);

        var resolvedCodeOwners = await ResolveCodeOwnersAsync(codeOwners.CodeOwners);

        // Get existing reviewers to avoid duplicates
        var existingReviewerIds = azureDevOpsPr.Reviewers?.Select(r => r.Id).ToHashSet() ?? [];

        var reviewersToAdd = resolvedCodeOwners
            .Where(x => !string.IsNullOrEmpty(x.AzureDevOpsId) && !existingReviewerIds.Contains(x.AzureDevOpsId))
            .ToList();

        if (reviewersToAdd.Count == 0)
        {
            _logger.LogWarning(
                "No code owner reviewers added to PR {PullRequestId}: {ResolvedCount}/{TotalCount} owners resolved to an identity, remainder already reviewers",
                pullRequestId,
                resolvedCodeOwners.Count(x => !string.IsNullOrEmpty(x.AzureDevOpsId)),
                resolvedCodeOwners.Count);
            return;
        }

        foreach (var reviewer in reviewersToAdd)
        {
            // Per-reviewer PUT — the batch POST endpoint ignores IsRequired
            await AddRequiredReviewerAsync(gitClient, projectName, repositoryName!, pullRequestId, reviewer);
        }
    }

    private async Task AddRequiredReviewerAsync(
        GitHttpClient gitClient,
        string projectName,
        string repositoryName,
        int pullRequestId,
        ResolvedCodeOwner reviewer)
    {
        try
        {
            await gitClient.CreatePullRequestReviewerAsync(
                new IdentityRefWithVote { Id = reviewer.AzureDevOpsId!, IsRequired = true },
                projectName,
                repositoryName,
                pullRequestId,
                reviewer.AzureDevOpsId!);

            _logger.LogInformation(
                "Added required reviewer {DisplayName} ({ReviewerId}) to PR {PullRequestId}",
                reviewer.DisplayName, reviewer.AzureDevOpsId, pullRequestId);
        }
        catch (VssServiceException ex)
        {
            _logger.LogError(ex,
                "Failed to add reviewer {DisplayName} ({ReviewerId}) to PR {PullRequestId}",
                reviewer.DisplayName, reviewer.AzureDevOpsId, pullRequestId);
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

            var identity = await FindIdentityAsync(identityClient, owner);

            if (identity is not null)
            {
                resolvedOwner.AzureDevOpsId = identity.Id.ToString();
                resolvedOwner.DisplayName = identity.DisplayName ?? identity.Id.ToString();

                // Determine if it's a team or user based on identity properties
                resolvedOwner.Type = identity.IsContainer ? CodeOwnerType.Team : CodeOwnerType.User;

                _logger.LogInformation(
                    "Resolved code owner {OwnerName} to {DisplayName} ({IdentityId})",
                    owner.Name, resolvedOwner.DisplayName, resolvedOwner.AzureDevOpsId);
            }
            else
            {
                _logger.LogWarning(
                    "Could not resolve code owner {OwnerName} (type {OwnerType}) to an Azure DevOps identity",
                    owner.Name, owner.Type);
            }

            resolvedOwners.Add(resolvedOwner);
        }

        return resolvedOwners;
    }

    /// <summary>
    /// Looks up an Azure DevOps identity for a CODEOWNERS entry: email addresses use the
    /// MailAddress filter (General only prefix-matches display/account name), everything else
    /// falls back to a General search on the name.
    /// </summary>
    private static async Task<Identity?> FindIdentityAsync(IdentityHttpClient identityClient, CodeOwner owner)
    {
        var email = owner.Email ?? (owner.Type == CodeOwnerType.Email ? owner.Name : null);

        if (!string.IsNullOrEmpty(email))
        {
            var byEmail = await identityClient.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, email);
            if (byEmail.Any())
            {
                return byEmail.First();
            }
        }

        var byName = await identityClient.ReadIdentitiesAsync(IdentitySearchFilter.General, owner.Name);
        return byName.FirstOrDefault();
    }


    public async Task<PullRequestCommentThread> GetPullRequestThreadContextAsync(string projectName, string repositoryName, int pullRequestId, int prCommentId)
    {
        var gitClient = await GetHttpGitClient();

        var thread = await gitClient.GetPullRequestThreadAsync(projectName, repositoryName, pullRequestId, prCommentId);

        return MapToGenericCommentThread(thread);
    }

    /// <summary>
    /// Maps an Azure DevOps GitPullRequest to the generic PullRequest model.
    /// </summary>
    private static PullRequest MapToGenericPullRequest(Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequest azureDevOpsPr)
    {
        return new PullRequest
        {
            PullRequestId = azureDevOpsPr.PullRequestId,
            Title = azureDevOpsPr.Title,
            Description = azureDevOpsPr.Description,
            SourceRefName = azureDevOpsPr.SourceRefName,
            TargetRefName = azureDevOpsPr.TargetRefName,
            Status = MapPullRequestStatus(azureDevOpsPr.Status),
            CreatedBy = azureDevOpsPr.CreatedBy != null ? MapToGenericIdentityRef(azureDevOpsPr.CreatedBy) : null,
            CreationDate = azureDevOpsPr.CreationDate,
            LastMergeCommit = azureDevOpsPr.LastMergeCommit != null ? MapToGenericCommitRef(azureDevOpsPr.LastMergeCommit) : null,
            SourceCommit = azureDevOpsPr.LastMergeSourceCommit != null ? MapToGenericCommitRef(azureDevOpsPr.LastMergeSourceCommit) : null
        };
    }

    /// <summary>
    /// Maps an Azure DevOps GitPullRequestCommentThread to the generic PullRequestCommentThread model.
    /// </summary>
    private static PullRequestCommentThread MapToGenericCommentThread(GitPullRequestCommentThread azureDevOpsThread)
    {
        return new PullRequestCommentThread
        {
            Id = azureDevOpsThread.Id,
            Comments = azureDevOpsThread.Comments?.Select(MapToGenericComment).ToList() ?? [],
            Status = MapCommentThreadStatus(azureDevOpsThread.Status),
            ThreadContext = azureDevOpsThread.ThreadContext != null ? MapToGenericThreadContext(azureDevOpsThread.ThreadContext) : null
        };
    }

    /// <summary>
    /// Maps an Azure DevOps Comment to the generic PullRequestComment model.
    /// </summary>
    private static PullRequestComment MapToGenericComment(Comment azureDevOpsComment)
    {
        return new PullRequestComment
        {
            Id = azureDevOpsComment.Id,
            ParentCommentId = azureDevOpsComment.ParentCommentId,
            Content = azureDevOpsComment.Content ?? string.Empty,
            Author = azureDevOpsComment.Author != null ? MapToGenericIdentityRef(azureDevOpsComment.Author) : null,
            PublishedDate = azureDevOpsComment.PublishedDate,
            LastUpdatedDate = azureDevOpsComment.LastUpdatedDate,
            CommentType = MapCommentType(azureDevOpsComment.CommentType)
        };
    }

    /// <summary>
    /// Maps an Azure DevOps IdentityRef to the generic IdentityRef model.
    /// </summary>
    private static Lintellect.Api.Application.Models.Git.IdentityRef MapToGenericIdentityRef(Microsoft.VisualStudio.Services.WebApi.IdentityRef azureDevOpsIdentity)
    {
        return new Lintellect.Api.Application.Models.Git.IdentityRef
        {
            DisplayName = azureDevOpsIdentity.DisplayName,
            UniqueName = azureDevOpsIdentity.UniqueName,
            Id = azureDevOpsIdentity.Id?.ToString(),
            Url = azureDevOpsIdentity.Url,
            ImageUrl = azureDevOpsIdentity.ImageUrl
        };
    }

    /// <summary>
    /// Maps an Azure DevOps GitCommitRef to the generic CommitRef model.
    /// </summary>
    private static CommitRef MapToGenericCommitRef(Microsoft.TeamFoundation.SourceControl.WebApi.GitCommitRef azureDevOpsCommit)
    {
        return new CommitRef
        {
            CommitId = azureDevOpsCommit.CommitId,
            Comment = azureDevOpsCommit.Comment,
            Author = azureDevOpsCommit.Author != null ? new Application.Models.Git.IdentityRef
            {
                DisplayName = azureDevOpsCommit.Author.Name,
                UniqueName = azureDevOpsCommit.Author.Name
            } : null,
            Committer = azureDevOpsCommit.Committer != null ? new Application.Models.Git.IdentityRef
            {
                DisplayName = azureDevOpsCommit.Committer.Name,
                UniqueName = azureDevOpsCommit.Committer.Name
            } : null,
            CommitDate = azureDevOpsCommit.Author?.Date ?? azureDevOpsCommit.Committer?.Date
        };
    }

    /// <summary>
    /// Maps an Azure DevOps CommentThreadContext to the generic CommentThreadContext model.
    /// </summary>
    private static Application.Models.Git.CommentThreadContext MapToGenericThreadContext(Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadContext azureDevOpsContext)
    {
        return new Application.Models.Git.CommentThreadContext
        {
            FilePath = azureDevOpsContext.FilePath ?? string.Empty,
            RightFileStart = azureDevOpsContext.RightFileStart != null ? MapToGenericCommentPosition(azureDevOpsContext.RightFileStart) : null,
            RightFileEnd = azureDevOpsContext.RightFileEnd != null ? MapToGenericCommentPosition(azureDevOpsContext.RightFileEnd) : null
        };
    }

    /// <summary>
    /// Maps an Azure DevOps CommentPosition to the generic CommentPosition model.
    /// </summary>
    private static Application.Models.Git.CommentPosition MapToGenericCommentPosition(Microsoft.TeamFoundation.SourceControl.WebApi.CommentPosition azureDevOpsPosition)
    {
        return new Application.Models.Git.CommentPosition
        {
            Line = azureDevOpsPosition.Line,
            Offset = azureDevOpsPosition.Offset
        };
    }

    /// <summary>
    /// Maps an Azure DevOps PullRequestStatus to the generic PullRequestStatus enum.
    /// </summary>
    private static Application.Models.Git.PullRequestStatus MapPullRequestStatus(Microsoft.TeamFoundation.SourceControl.WebApi.PullRequestStatus? status)
    {
        return status switch
        {
            Microsoft.TeamFoundation.SourceControl.WebApi.PullRequestStatus.Active => Application.Models.Git.PullRequestStatus.Active,
            Microsoft.TeamFoundation.SourceControl.WebApi.PullRequestStatus.Completed => Application.Models.Git.PullRequestStatus.Completed,
            Microsoft.TeamFoundation.SourceControl.WebApi.PullRequestStatus.Abandoned => Application.Models.Git.PullRequestStatus.Abandoned,
            _ => Application.Models.Git.PullRequestStatus.Active
        };
    }

    /// <summary>
    /// Maps an Azure DevOps CommentThreadStatus to the generic CommentThreadStatus enum.
    /// </summary>
    private static Application.Models.Git.CommentThreadStatus MapCommentThreadStatus(Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus? status)
    {
        return status switch
        {
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Active => Application.Models.Git.CommentThreadStatus.Active,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Fixed => Application.Models.Git.CommentThreadStatus.Resolved,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.WontFix => Application.Models.Git.CommentThreadStatus.Resolved,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.ByDesign => Application.Models.Git.CommentThreadStatus.Resolved,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus.Closed => Application.Models.Git.CommentThreadStatus.Closed,
            _ => Application.Models.Git.CommentThreadStatus.Active
        };
    }

    /// <summary>
    /// Maps an Azure DevOps CommentType to the generic CommentType enum.
    /// </summary>
    private static Application.Models.Git.CommentType MapCommentType(Microsoft.TeamFoundation.SourceControl.WebApi.CommentType? commentType)
    {
        return commentType switch
        {
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.Text => Application.Models.Git.CommentType.Text,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.CodeChange => Application.Models.Git.CommentType.CodeChange,
            Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.System => Application.Models.Git.CommentType.System,
            _ => Application.Models.Git.CommentType.Text
        };
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
