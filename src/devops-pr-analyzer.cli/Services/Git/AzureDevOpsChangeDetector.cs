using devops_pr_analyzer.cli.Extensions;
using devops_pr_analyzer.cli.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Services.Git;

internal sealed class AzureDevOpsChangeDetector : ICodeChangeDetector
{
    public IReadOnlySet<string> GetChangedFiles(IEnumerable<string>? includePatterns = null)
    {
        var repoDir = Env("BUILD_SOURCESDIRECTORY") ?? Directory.GetCurrentDirectory();
        var source = Env("SYSTEM_PULLREQUEST_SOURCEBRANCH");
        var target = Env("SYSTEM_PULLREQUEST_TARGETBRANCH");

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return new HashSet<string>();

        target = target.Replace("refs/heads/", "origin/");
        source = source.Replace("refs/heads/", "origin/");

        var files = ChangeDetectorExtensions.RunGitDiff(repoDir, target, source, includePatterns);
        return ChangeDetectorExtensions.ToSet(repoDir, files);
    }

    private static string? Env(string k) => Environment.GetEnvironmentVariable(k);


}

