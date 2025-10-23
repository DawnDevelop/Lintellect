using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Services.Git;

internal static class GitInfoExtractorFactory
{
    public static IGitInfoExtractor Create()
    {
        if (Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI") != null)
            return new AzureDevOpsInfoExtractor();

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            return new GitHubInfoExtractor();

        // default: local or unknown → analyze all
        return new NoOpChangeDetector();
    }
}

internal sealed class NoOpChangeDetector : IGitInfoExtractor
{
    public GitInfo? ExtractInfo() => null;
}