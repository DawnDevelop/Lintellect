namespace Lintellect.Shared.Models;

/// <summary>
/// Represents Git context information extracted from CI/CD pipeline environments.
/// Used to identify and track pull requests, builds, and repository information across different Git providers.
/// </summary>
/// <param name="PullRequestId">The unique identifier for the Git context (e.g., PR number, build ID, branch name)</param>
/// <param name="CommitId">The Git commit SHA associated with this context</param>
/// <param name="RepositoryName">The name of the repository in the format 'owner/repo' or full repository path</param>
/// <param name="Type">The type of Git information context (PullRequest, CIBuild, ManualBuild, or Unknown)</param>
/// <param name="ProjectName">Optional project name for platforms that organize repositories into projects (e.g., Azure DevOps)</param>
public record GitInfo(int PullRequestId, string CommitId, string RepositoryName, EGitInfoType Type = EGitInfoType.Unknown, string? ProjectName = null);
