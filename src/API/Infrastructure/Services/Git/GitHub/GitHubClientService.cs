using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Application.Models;
using devops_pr_analyzer.shared.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Octokit;
using System.Net.Http;

namespace devops_pr_analyzer.Infrastructure.Services.Git.GitHub;

/// <summary>
/// GitHub client implementation using Octokit.NET following CleanArchitecture pattern.
/// </summary>
public sealed class GitHubClientService : IGitClient
{
    private readonly GitHubClient _client;
    private readonly ILogger<GitHubClientService> _logger;

    public GitHubClientService(string token, ILogger<GitHubClientService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var productHeader = new ProductHeaderValue("DevOps-PR-Analyzer");
        _client = new GitHubClient(productHeader)
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<Dictionary<string, string>> GetPullRequestCompactDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        int contextLines = 3,
        int maxNewFileLines = 50,
        int maxLinesPerFile = 1000)
    {
        try
        {
            _logger.LogInformation("Retrieving GitHub PR #{PullRequestId} diffs for {Owner}/{Repo}",
                pullRequestId, projectName, repositoryName);

            var pr = await _client.PullRequest.Get(projectName, repositoryName, pullRequestId);
            var files = await _client.PullRequest.Files(projectName, repositoryName, pullRequestId);

            var diffs = new Dictionary<string, string>();

            foreach (var file in files)
            {
                if (ShouldSkipFile(file, maxLinesPerFile))
                    continue;

                var diff = await GetFileDiffAsync(file, contextLines, maxNewFileLines);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    diffs[file.FileName] = diff;
                }
            }

            _logger.LogInformation("Retrieved {FileCount} file diffs for GitHub PR #{PullRequestId}",
                diffs.Count, pullRequestId);

            return diffs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve GitHub PR #{PullRequestId} diffs", pullRequestId);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetPullRequestFileDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId)
    {
        try
        {
            _logger.LogInformation("Retrieving full GitHub PR #{PullRequestId} diffs for {Owner}/{Repo}",
                pullRequestId, projectName, repositoryName);

            var files = await _client.PullRequest.Files(projectName, repositoryName, pullRequestId);
            var diffs = new Dictionary<string, string>();

            foreach (var file in files)
            {
                var diff = await GetFullFileDiffAsync(file);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    diffs[file.FileName] = diff;
                }
            }

            return diffs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve full GitHub PR #{PullRequestId} diffs", pullRequestId);
            throw;
        }
    }

    public async Task<GitPullRequest> GetPullRequestAsync(
        string projectName,
        string repositoryName,
        int pullRequestId)
    {
        try
        {
            var pr = await _client.PullRequest.Get(projectName, repositoryName, pullRequestId);

            // Convert GitHub PR to Azure DevOps format for compatibility
            return new GitPullRequest
            {
                PullRequestId = pullRequestId,
                Title = pr.Title,
                Description = pr.Body,
                SourceRefName = $"refs/heads/{pr.Head.Ref}",
                TargetRefName = $"refs/heads/{pr.Base.Ref}",
                Status = PullRequestStatus.Active,
                CreatedBy = new IdentityRef
                {
                    DisplayName = pr.User.Login,
                    UniqueName = pr.User.Login
                },
                CreationDate = pr.CreatedAt.DateTime,
                LastMergeCommit = new GitCommitRef
                {
                    CommitId = pr.Head.Sha
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve GitHub PR #{PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<string?> GetFileAsync(
        string projectName,
        string repositoryName,
        string? branchName = null,
        params string[] possiblePaths)
    {
        try
        {
            var branch = branchName ?? "main";

            foreach (var path in possiblePaths)
            {
                try
                {
                    var content = await _client.Repository.Content.GetAllContentsByRef(projectName, repositoryName, path, branch);
                    if (content.Count > 0)
                    {
                        return content[0].Content;
                    }
                }
                catch (NotFoundException)
                {
                    // File not found, try next path
                    continue;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve file from GitHub repository {Owner}/{Repo}",
                projectName, repositoryName);
            throw;
        }
    }

    public async Task<GitPullRequestCommentThread> CreateCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string comment)
    {
        try
        {
            var issueComment = await _client.Issue.Comment.Create(
                projectName,
                repositoryName,
                pullRequestId,
                comment);

            // Convert to Azure DevOps format for compatibility
            return new GitPullRequestCommentThread
            {
                Id = (int)issueComment.Id,
                Comments =
                [
                    new() {
                        Id = (short)issueComment.Id,
                        Content = issueComment.Body,
                        Author = new IdentityRef
                        {
                            DisplayName = issueComment.User.Login,
                            UniqueName = issueComment.User.Login
                        },
                        PublishedDate = issueComment.CreatedAt.DateTime
                    }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment on GitHub PR #{PullRequestId}", pullRequestId);
            throw;
        }
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
        try
        {
            if (filePath != null && lineFrom.HasValue)
            {
                // Create a review comment (inline suggestion)
                var reviewComment = await _client.PullRequest.ReviewComment.Create(
                    projectName,
                    repositoryName,
                    pullRequestId,
                    new PullRequestReviewCommentCreate(codeChange, "unknown", "unknown", 1));

                return new GitPullRequestCommentThread
                {
                    Id = (int)reviewComment.Id,
                    Comments =
                    [
                        new Comment
                        {
                            Id = (short)reviewComment.Id,
                            Content = $"{context}\n\n```suggestion\n{codeChange}\n```",
                            Author = new IdentityRef
                            {
                                DisplayName = reviewComment.User.Login,
                                UniqueName = reviewComment.User.Login
                            },
                            PublishedDate = reviewComment.CreatedAt.DateTime
                        }
                    ]
                };
            }
            else
            {
                // Create a general comment
                return await CreateCommentAsync(projectName, repositoryName, pullRequestId,
                    $"{context}\n\n```\n{codeChange}\n```");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create code change comment on GitHub PR #{PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<GitPullRequest> AppendToDescriptionAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string textToAppend,
        string separator = "\n\n---\n\n")
    {
        try
        {
            var pr = await _client.PullRequest.Get(projectName, repositoryName, pullRequestId);
            var newDescription = pr.Body + separator + textToAppend;

            var update = new PullRequestUpdate
            {
                Body = newDescription
            };

            var updatedPr = await _client.PullRequest.Update(projectName, repositoryName, pullRequestId, update);

            return new GitPullRequest
            {
                PullRequestId = pullRequestId,
                Title = updatedPr.Title,
                Description = updatedPr.Body,
                SourceRefName = $"refs/heads/{updatedPr.Head.Ref}",
                TargetRefName = $"refs/heads/{updatedPr.Base.Ref}",
                Status = PullRequestStatus.Active,
                CreatedBy = new IdentityRef
                {
                    DisplayName = updatedPr.User.Login,
                    UniqueName = updatedPr.User.Login
                },
                CreationDate = updatedPr.CreatedAt.DateTime,
                LastMergeCommit = new GitCommitRef
                {
                    CommitId = updatedPr.Head.Sha
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append to GitHub PR #{PullRequestId} description", pullRequestId);
            throw;
        }
    }

    private static bool ShouldSkipFile(PullRequestFile file, int maxLinesPerFile)
    {
        // Skip binary files
        if (file.Status == "added" && file.Changes == 0)
            return true;

        // Skip files that are too large
        if (file.Changes > maxLinesPerFile)
            return true;

        // Skip common build artifacts
        var fileName = Path.GetFileName(file.FileName).ToLowerInvariant();
        return fileName.EndsWith(".min.js") ||
               fileName.EndsWith(".min.css") ||
               fileName.Contains("node_modules") ||
               fileName.Contains("bin/") ||
               fileName.Contains("obj/");
    }

    private async Task<string> GetFileDiffAsync(PullRequestFile file, int contextLines, int maxNewFileLines)
    {
        try
        {
            if (file.Status == "added" && file.Changes > maxNewFileLines)
            {
                // For new files, limit the content
                var content = await _client.Repository.Content.GetAllContentsByRef(
                    file.FileName.Split('/')[0],
                    file.FileName.Split('/')[1],
                    file.FileName,
                    file.Sha);

                var lines = content[0].Content.Split('\n');
                var limitedLines = lines.Take(maxNewFileLines);
                return string.Join('\n', limitedLines);
            }

            return file.Patch ?? string.Empty;
        }
        catch
        {
            return file.Patch ?? string.Empty;
        }
    }

    private async Task<string> GetFullFileDiffAsync(PullRequestFile file)
    {
        try
        {
            if (file.Status == "added")
            {
                var content = await _client.Repository.Content.GetAllContentsByRef(
                    file.FileName.Split('/')[0],
                    file.FileName.Split('/')[1],
                    file.FileName,
                    file.Sha);

                return content[0].Content;
            }

            return file.Patch ?? string.Empty;
        }
        catch
        {
            return file.Patch ?? string.Empty;
        }
    }

    public Task AddCodeOwnersToPr(string projectName, int pullRequestId, CodeOwnersResult codeOwners, string? repositoryName = null)
    {
        // GitHub natively supports CODEOWNERS file - no implementation needed
        _logger.LogInformation("GitHub natively supports CODEOWNERS file. No additional implementation needed for PR #{PullRequestId}", pullRequestId);
        throw new NotSupportedException($"GitHub natively supports CODEOWNERS file. No additional implementation needed for PR #{pullRequestId}");
    }

    public async Task<List<CheckPermissionResult>> HasSufficientPermissionsAsync(AnalysisRequest analysisRequest)
    {
        var owner = analysisRequest.GitInfo!.ProjectName;
        var repoName = analysisRequest.GitInfo.RepositoryName;
        var pullRequestId = analysisRequest.GitInfo.PullRequestId;
        var results = new List<CheckPermissionResult>();

        try
        {
            // First, verify basic access to repository and pull request
            var repository = await _client.Repository.Get(owner, repoName);
            if (repository == null)
            {
                results.Add(new CheckPermissionResult(false, $"Repository '{owner}/{repoName}' not found"));
                return results;
            }

            var pullRequest = await _client.PullRequest.Get(owner, repoName, pullRequestId);
            if (pullRequest == null)
            {
                results.Add(new CheckPermissionResult(false, $"Pull request #{pullRequestId} not found in repository '{owner}/{repoName}'"));
                return results;
            }

            // Get token scopes by making a simple API call
            var scopes = await GetTokenScopesAsync();
            if (scopes == null || scopes.Count == 0)
            {
                results.Add(new CheckPermissionResult(false, "Unable to determine token scopes"));
                return results;
            }

            // Check required permissions based on enabled features
            // Repository read permission (always required)
            var hasRepoRead = scopes.Contains("repo") || scopes.Contains("public_repo");
            results.Add(new CheckPermissionResult(hasRepoRead, hasRepoRead ? null : "Repository Read: Missing 'repo' or 'public_repo' scope"));

            // Pull request read permission (always required - covered by repo scope)
            results.Add(new CheckPermissionResult(hasRepoRead, hasRepoRead ? null : "Pull Request Read: Missing 'repo' or 'public_repo' scope"));

            // Pull request comment permission (required for summary comments and inline suggestions)
            if (analysisRequest.EnableSummaryComment || analysisRequest.EnableInlineSuggestions)
            {
                var hasCommentScope = scopes.Contains("repo") || scopes.Contains("public_repo");
                results.Add(new CheckPermissionResult(hasCommentScope, hasCommentScope ? null : "Pull Request Comments: Missing 'repo' or 'public_repo' scope"));
            }

            // Pull request edit permission (required for description updates)
            if (analysisRequest.EnableDescriptionSummary)
            {
                var hasEditScope = scopes.Contains("repo") || scopes.Contains("public_repo");
                results.Add(new CheckPermissionResult(hasEditScope, hasEditScope ? null : "Pull Request Edit: Missing 'repo' or 'public_repo' scope"));
            }

            return results;
        }
        catch (Octokit.AuthorizationException)
        {
            results.Add(new CheckPermissionResult(false, "Authentication failed: Invalid or expired GitHub token"));
            return results;
        }
        catch (Octokit.ForbiddenException)
        {
            results.Add(new CheckPermissionResult(false, "Insufficient permissions: GitHub token lacks required scopes"));
            return results;
        }
        catch (Exception ex)
        {
            results.Add(new CheckPermissionResult(false, $"Permission check failed: {ex.Message}"));
            return results;
        }
    }

    private async Task<List<string>?> GetTokenScopesAsync()
    {
        try
        {
            // Make a simple API call to get the current user, which will include OAuth scopes in response headers
            var user = await _client.User.Current();

            // The scopes should be available in the response headers
            // Since we can't easily access them from Octokit, we'll use a different approach
            // We'll make a direct HTTP request to get the scopes
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _client.Credentials.GetToken());
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("DevOps-PR-Analyzer", "1.0"));

            var response = await httpClient.GetAsync("https://api.github.com");
            if (response.Headers.TryGetValues("X-OAuth-Scopes", out var scopeValues))
            {
                var scopesHeader = scopeValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(scopesHeader))
                {
                    return scopesHeader.Split(',').Select(s => s.Trim()).ToList();
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

}
