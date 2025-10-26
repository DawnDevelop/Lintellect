using Lintellect.Api.Application.Models;

namespace Lintellect.Api.Application.Interfaces;

public interface IAnalyzerServiceResolver
{
    IAnalyzerService GetAnalyzerService(EAnalyzers provider);
}
