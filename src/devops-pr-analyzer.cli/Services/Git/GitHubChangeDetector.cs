using devops_pr_analyzer.cli.Extensions;
using devops_pr_analyzer.cli.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Services.Git;

internal sealed class GitHubChangeDetector : ICodeChangeDetector
{
    public IReadOnlySet<string> GetChangedFiles(IEnumerable<string>? includePatterns = null)
    {
        var repoDir = Env("GITHUB_WORKSPACE") ?? Directory.GetCurrentDirectory();
        var baseRef = Env("GITHUB_BASE_REF");
        var headRef = Env("GITHUB_HEAD_REF");

        if (string.IsNullOrEmpty(baseRef) || string.IsNullOrEmpty(headRef))
            return new HashSet<string>();

        var files = ChangeDetectorExtensions.RunGitDiff(repoDir, $"origin/{baseRef}", $"origin/{headRef}", includePatterns);

        return ChangeDetectorExtensions.ToSet(repoDir, files);
    }

    private static string? Env(string k) => Environment.GetEnvironmentVariable(k);
}
