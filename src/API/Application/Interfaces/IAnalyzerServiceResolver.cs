using devops_pr_analyzer.Application.Models;

namespace devops_pr_analyzer.Application.Interfaces;

public interface IAnalyzerServiceResolver
{
    IAnalyzerService GetAnalyzerService(EAnalyzers provider);
}
