using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.cli.Services.Git;

internal sealed class AzureDevOpsInfoExtractor : IGitInfoExtractor
{
    public GitInfo? ExtractInfo()
    {
        var pullRequestId = Env("SYSTEM_PULLREQUEST_PULLREQUESTID");
        var commitId = Env("BUILD_SOURCEVERSION");
        var repositoryName = Env("BUILD_REPOSITORY_NAME");
        var buildReason = Env("BUILD_REASON");
        var projectName = Env("SYSTEM_TEAMPROJECT");

        if (string.IsNullOrWhiteSpace(commitId) || string.IsNullOrWhiteSpace(repositoryName))
            return null;

        // Determine build type
        if (!int.TryParse(pullRequestId, out var result))
        {
            return new GitInfo(result, commitId, repositoryName, EGitInfoType.PullRequest);
        }
        else
        {
            Console.WriteLine("No Pull Request detected in the current build environment.");
        }

        var buildId = Env("BUILD_BUILDID");

        if(!int.TryParse(buildId, out var parsedBuildId))
        {
            parsedBuildId = -1;
            Console.WriteLine("Warning: Unable to parse BUILD_BUILDID environment variable.");
        }

        var type = buildReason switch
        {
            "IndividualCI" => EGitInfoType.CIBuild,
            "Manual" => EGitInfoType.ManualBuild,
            _ => EGitInfoType.Unknown
        };

        return new GitInfo(parsedBuildId, commitId, repositoryName, type, ProjectName: projectName);
    }

    private static string? Env(string k) => Environment.GetEnvironmentVariable(k);

}

