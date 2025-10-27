using Lintellect.Shared.Models;

namespace Lintellect.Cli.Interfaces;

internal interface ICodeAnalyzer
{
    EProgrammingLanguage Language { get; }
    Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath);
}
