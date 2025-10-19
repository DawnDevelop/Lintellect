using devops_pr_analyzer.cli.Extensions;
using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Services.Git;

internal sealed class AzureDevOpsInfoExtractor : IGitInfoExtractor
{
    public GitInfo? ExtractInfo()
    {
        var pullRequestId = Env("SYSTEM_PULLREQUEST_PULLREQUESTID");
        var commitId = Env("BUILD_SOURCEVERSION");
        var repositoryName = Env("BUILD_REPOSITORY_NAME");
        var buildReason = Env("BUILD_REASON");

        if (string.IsNullOrWhiteSpace(commitId) || string.IsNullOrWhiteSpace(repositoryName))
            return null;

        // Determine build type
        if (!string.IsNullOrWhiteSpace(pullRequestId))
        {
            return new GitInfo(pullRequestId, commitId, repositoryName, GitInfoType.PullRequest);
        }

        var buildId = Env("BUILD_BUILDID") ?? "unknown";
        var type = buildReason switch
        {
            "IndividualCI" => GitInfoType.CIBuild,
            "Manual" => GitInfoType.ManualBuild,
            _ => GitInfoType.Unknown
        };

        return new GitInfo(buildId, commitId, repositoryName, type);
    }

    private static string? Env(string k) => Environment.GetEnvironmentVariable(k);

}

