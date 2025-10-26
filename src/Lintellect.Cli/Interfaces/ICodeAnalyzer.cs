using Lintellect.Shared.Models;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintellect.Cli.Interfaces;

internal interface ICodeAnalyzer
{
    EProgrammingLanguage Language { get; }
    Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath);
}
