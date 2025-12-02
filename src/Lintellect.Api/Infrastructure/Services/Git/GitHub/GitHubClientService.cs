using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Shared.Models;
using Octokit;

namespace Lintellect.Api.Infrastructure.Services.Git.GitHub;

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

        var productHeader = new ProductHeaderValue("Lintellect");
        _client = new GitHubClient(productHeader)
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<Dictionary<string, string>> GetPullRequestCompactDiffsAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        int contextLines)
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
                if (ShouldSkipFile(file))
                {
                    continue;
                }

                var diff = await GetFileDiffAsync(file, contextLines);
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

    public async Task<Lintellect.Api.Application.Models.Git.PullRequest> GetPullRequestAsync(
        string projectName,
        string repositoryName,
        int pullRequestId)
    {
        try
        {
            var pr = await _client.PullRequest.Get(projectName, repositoryName, pullRequestId);

            return new Lintellect.Api.Application.Models.Git.PullRequest
            {
                PullRequestId = pullRequestId,
                Title = pr.Title,
                Description = pr.Body,
                SourceRefName = $"refs/heads/{pr.Head.Ref}",
                TargetRefName = $"refs/heads/{pr.Base.Ref}",
                Status = MapPullRequestStatus(pr.State),
                CreatedBy = new IdentityRef
                {
                    DisplayName = pr.User.Login,
                    UniqueName = pr.User.Login,
                    Id = pr.User.Id.ToString(),
                    Url = pr.User.HtmlUrl,
                    ImageUrl = pr.User.AvatarUrl
                },
                CreationDate = pr.CreatedAt.UtcDateTime,
                LastMergeCommit = new CommitRef
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

    public async Task<PullRequestCommentThread> CreateCommentAsync(
        string projectName,
        string repositoryName,
        int pullRequestId,
        string comment, int? threadId = null)
    {
        try
        {
            var issueComment = await _client.Issue.Comment.Create(
                projectName,
                repositoryName,
                pullRequestId,
                comment);

            return new PullRequestCommentThread
            {
                Id = (int)issueComment.Id,
                Comments =
                [
                    new PullRequestComment
                    {
                        Id = (int)issueComment.Id,
                        Content = issueComment.Body ?? string.Empty,
                        Author = new IdentityRef
                        {
                            DisplayName = issueComment.User.Login,
                            UniqueName = issueComment.User.Login,
                            Id = issueComment.User.Id.ToString(),
                            Url = issueComment.User.HtmlUrl,
                            ImageUrl = issueComment.User.AvatarUrl
                        },
                        PublishedDate = issueComment.CreatedAt.UtcDateTime,
                        LastUpdatedDate = issueComment.UpdatedAt?.UtcDateTime,
                        CommentType = CommentType.Text
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


    public async Task<PullRequestCommentThread> GetPullRequestThreadContextAsync(string projectName, string repositoryName, int pullRequestId, int prCommentId)
    {
        try
        {
            // GitHub API does not have "thread" resources in the same way as Azure DevOps.
            // prCommentId corresponds to the GitHub comment ID.
            // To get a single issue comment, we only need owner, repo, and commentId (not the issue/PR number)
            var comment = await _client.Issue.Comment.Get(projectName, repositoryName, prCommentId);

            return new PullRequestCommentThread
            {
                Id = (int)comment.Id,
                Comments =
                [
                    new PullRequestComment
                {
                    Id = (int)comment.Id,
                    Content = comment.Body ?? string.Empty,
                    Author = new IdentityRef
                    {
                        DisplayName = comment.User.Login,
                        UniqueName = comment.User.Login,
                        Id = comment.User.Id.ToString(),
                        Url = comment.User.HtmlUrl,
                        ImageUrl = comment.User.AvatarUrl
                    },
                    PublishedDate = comment.CreatedAt.UtcDateTime,
                    LastUpdatedDate = comment.UpdatedAt?.UtcDateTime,
                    CommentType = CommentType.Text
                }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve GitHub PR #{PullRequestId} comment #{CommentId}", pullRequestId, prCommentId);
            throw;
        }
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
        try
        {
            if (filePath != null && lineFrom.HasValue)
            {
                // Create a review comment (inline suggestion)
                // Note: GitHub's API requires the diff_hunk and position, which we need to calculate
                // For now, we'll create a general comment with the suggestion format
                var commentText = string.IsNullOrWhiteSpace(context)
                    ? $"```suggestion\n{codeChange}\n```"
                    : $"{context}\n\n```suggestion\n{codeChange}\n```";

                var reviewComment = await _client.PullRequest.ReviewComment.Create(
                    projectName,
                    repositoryName,
                    pullRequestId,
                    new PullRequestReviewCommentCreate(commentText, filePath, filePath, lineFrom.Value));

                return new PullRequestCommentThread
                {
                    Id = (int)reviewComment.Id,
                    Comments =
                    [
                        new PullRequestComment
                        {
                            Id = (int)reviewComment.Id,
                            Content = reviewComment.Body ?? string.Empty,
                            Author = new IdentityRef
                            {
                                DisplayName = reviewComment.User.Login,
                                UniqueName = reviewComment.User.Login,
                                Id = reviewComment.User.Id.ToString(),
                                Url = reviewComment.User.HtmlUrl,
                                ImageUrl = reviewComment.User.AvatarUrl
                            },
                            PublishedDate = reviewComment.CreatedAt.UtcDateTime,
                            LastUpdatedDate = reviewComment.UpdatedAt.UtcDateTime,
                            CommentType = CommentType.CodeChange
                        }
                    ],
                    ThreadContext = new CommentThreadContext
                    {
                        FilePath = filePath,
                        RightFileStart = new CommentPosition
                        {
                            Line = lineFrom.Value,
                            Offset = 1
                        },
                        RightFileEnd = new CommentPosition
                        {
                            Line = lineTo ?? lineFrom.Value,
                            Offset = 1
                        }
                    }
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

    public async Task<Lintellect.Api.Application.Models.Git.PullRequest> AppendToDescriptionAsync(
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

            return new Lintellect.Api.Application.Models.Git.PullRequest
            {
                PullRequestId = pullRequestId,
                Title = updatedPr.Title,
                Description = updatedPr.Body,
                SourceRefName = $"refs/heads/{updatedPr.Head.Ref}",
                TargetRefName = $"refs/heads/{updatedPr.Base.Ref}",
                Status = MapPullRequestStatus(updatedPr.State),
                CreatedBy = new IdentityRef
                {
                    DisplayName = updatedPr.User.Login,
                    UniqueName = updatedPr.User.Login,
                    Id = updatedPr.User.Id.ToString(),
                    Url = updatedPr.User.HtmlUrl,
                    ImageUrl = updatedPr.User.AvatarUrl
                },
                CreationDate = updatedPr.CreatedAt.UtcDateTime,
                LastMergeCommit = new CommitRef
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

    private static bool ShouldSkipFile(PullRequestFile file)
    {
        // Skip binary files
        if (file.Status == "added" && file.Changes == 0)
        {
            return true;
        }

        // Skip common build artifacts
        var fileName = Path.GetFileName(file.FileName).ToLowerInvariant();
        return fileName.EndsWith(".min.js") ||
               fileName.EndsWith(".min.css") ||
               fileName.Contains("node_modules") ||
               fileName.Contains("bin/") ||
               fileName.Contains("obj/");
    }

    private async Task<string> GetFileDiffAsync(PullRequestFile file, int contextLines)
    {
        try
        {
            if (file.Status == "added")
            {
                // For new files, limit the content
                var content = await _client.Repository.Content.GetAllContentsByRef(
                    file.FileName.Split('/')[0],
                    file.FileName.Split('/')[1],
                    file.FileName,
                    file.Sha);

                var lines = content[0].Content.Split('\n');
                return string.Join('\n', lines);
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
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Lintellect", "1.0"));

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

    /// <summary>
    /// Maps a GitHub PullRequestState to the generic PullRequestStatus enum.
    /// </summary>
    private static Lintellect.Api.Application.Models.Git.PullRequestStatus MapPullRequestStatus(StringEnum<ItemState> state)
    {
        return state.Value switch
        {
            ItemState.Open => Lintellect.Api.Application.Models.Git.PullRequestStatus.Active,
            ItemState.Closed => Lintellect.Api.Application.Models.Git.PullRequestStatus.Completed,
            _ => Lintellect.Api.Application.Models.Git.PullRequestStatus.Active
        };
    }

}
