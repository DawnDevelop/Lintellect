using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.UnitTests.Mocks;

internal class MockGitInfoExtractor : IGitInfoExtractor
{
    public GitInfo? GitInfo { get; set; }

    public GitInfo? ExtractInfo()
    {
        return GitInfo;
    }
}
