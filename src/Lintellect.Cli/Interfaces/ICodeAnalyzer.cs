using Lintellect.Shared.Models;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintellect.Cli.Interfaces;

internal interface ICodeAnalyzer
{
    EProgrammingLanguage Language { get; }
    Task<Shared.Models.AnalysisRequest> AnalyzeAsync(string solutionPath);
}
