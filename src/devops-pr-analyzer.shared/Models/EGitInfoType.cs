using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.shared.Models;

public enum EGitInfoType
{
    Unknown,
    PullRequest,
    CIBuild,
    ManualBuild
}
