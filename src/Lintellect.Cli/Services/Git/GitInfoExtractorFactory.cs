using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services.Git;

internal static class GitInfoExtractorFactory
{
    public static IGitInfoExtractor Create()
    {
        if (Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI") != null)
        {
            return new AzureDevOpsInfoExtractor();
        }

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            return new GitHubInfoExtractor();
        }

        // default: local or unknown → analyze all
        return new NoOpChangeDetector();
    }
}

internal sealed class NoOpChangeDetector : IGitInfoExtractor
{
    public GitInfo? ExtractInfo()
    {
        return null;
    }
}
