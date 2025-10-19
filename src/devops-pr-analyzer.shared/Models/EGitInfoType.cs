using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.shared.Models;

public enum GitInfoType
{
    Unknown,
    PullRequest,
    CIBuild,
    ManualBuild
}
