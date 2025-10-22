using devops_pr_analyzer.Models;

namespace devops_pr_analyzer.Interfaces;

public interface IAnalyzerServiceResolver
{
    IAnalyzerService GetAnalyzerService(EAnalyzers provider);
}
