using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.shared.Models;

public record GitInfo(string Identifier, string CommitId, string RepositoryName, GitInfoType Type = GitInfoType.Unknown);