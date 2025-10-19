using devops_pr_analyzer.cli.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Services.Git;

internal static class CodeChangeDetectorFactory
{
    public static ICodeChangeDetector Create(IEnumerable<string>? includePatterns = null)
    {
        if (Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI") != null)
            return new AzureDevOpsChangeDetector();

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            return new GitHubChangeDetector();

        // default: local or unknown → analyze all
        return new NoOpChangeDetector();
    }
}

internal sealed class NoOpChangeDetector : ICodeChangeDetector
{
    public IReadOnlySet<string> GetChangedFiles(IEnumerable<string>? includePatterns = null) => new HashSet<string>();
}