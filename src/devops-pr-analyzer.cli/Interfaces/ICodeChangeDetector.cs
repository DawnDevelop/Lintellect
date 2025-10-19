using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Interfaces;

internal interface ICodeChangeDetector
{
    IReadOnlySet<string> GetChangedFiles(IEnumerable<string>? includePatterns);
}
