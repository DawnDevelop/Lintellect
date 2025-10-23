using devops_pr_analyzer.shared.Models;
using Microsoft.CodeAnalysis.Diagnostics;

namespace devops_pr_analyzer.cli.Interfaces;

internal interface ICodeAnalyzer
{
    EProgrammingLanguage Language { get; }
    Task<shared.Models.AnalysisResult> AnalyzeAsync(string solutionPath);
}