using devops_pr_analyzer.shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Interfaces;

internal interface IGitInfoExtractor
{
    GitInfo? ExtractInfo();
}
