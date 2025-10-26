using Lintellect.Shared.Models;

namespace Lintellect.Cli.Interfaces;

internal interface IGitInfoExtractor
{
    GitInfo? ExtractInfo();
}
