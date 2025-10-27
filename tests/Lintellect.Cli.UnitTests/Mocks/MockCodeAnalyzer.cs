using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.UnitTests.Mocks;

internal class MockCodeAnalyzer : ICodeAnalyzer
{
    public EProgrammingLanguage Language { get; set; } = EProgrammingLanguage.CSharp;
    public List<AnalyzerFindings> Findings { get; set; } = [];
    public bool ShouldThrowException { get; set; } = false;
    public string? ExceptionMessage { get; set; }

    public Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath)
    {
        return ShouldThrowException ? throw new FileNotFoundException(ExceptionMessage ?? "Mock exception") : Task.FromResult(Findings);
    }
}
