using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services.Git;

internal sealed class GitHubInfoExtractor : IGitInfoExtractor
{
    public GitInfo? ExtractInfo()
    {
        var gitHubRef = Env("GITHUB_REF");
        var commitId = Env("GITHUB_SHA");
        var repositoryName = Env("GITHUB_REPOSITORY");

        if (string.IsNullOrWhiteSpace(gitHubRef) ||
            string.IsNullOrWhiteSpace(commitId) ||
            string.IsNullOrWhiteSpace(repositoryName))
        {
            return null;
        }

        // Extract PR number from GITHUB_REF (format: refs/pull/{pr_number}/merge)
        var pullRequestId = ExtractPullRequestNumber(gitHubRef);
        return string.IsNullOrWhiteSpace(pullRequestId) ? null : new GitInfo(int.Parse(pullRequestId), commitId, repositoryName);
    }

    private static string? ExtractPullRequestNumber(string gitHubRef)
    {
        // Format: refs/pull/{pr_number}/merge or refs/pull/{pr_number}/head
        if (gitHubRef.StartsWith("refs/pull/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = gitHubRef.Split('/');
            if (parts.Length >= 3)
            {
                return parts[2];
            }
        }
        return null;
    }

    private static string? Env(string k)
    {
        return Environment.GetEnvironmentVariable(k);
    }
}
