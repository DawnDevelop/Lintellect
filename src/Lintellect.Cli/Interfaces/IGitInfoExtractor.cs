using Lintellect.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lintellect.Cli.Interfaces;

internal interface IGitInfoExtractor
{
    GitInfo? ExtractInfo();
}
