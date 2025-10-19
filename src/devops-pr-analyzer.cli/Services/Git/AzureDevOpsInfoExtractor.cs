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

        if (string.IsNullOrWhiteSpace(pullRequestId) || 
            string.IsNullOrWhiteSpace(commitId) || 
            string.IsNullOrWhiteSpace(repositoryName))
        {
            return null;
        }

        return new GitInfo(pullRequestId, commitId, repositoryName);
    }

    private static string? Env(string k) => Environment.GetEnvironmentVariable(k);

}

