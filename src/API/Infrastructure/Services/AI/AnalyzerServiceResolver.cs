using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Application.Models;
using devops_pr_analyzer.shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace devops_pr_analyzer.Infrastructure.Services.AI;

internal sealed class AnalyzerServiceResolver(IServiceProvider serviceProvider) : IAnalyzerServiceResolver
{
    public IAnalyzerService GetAnalyzerService(EAnalyzers provider)
    {
        var client = serviceProvider.GetKeyedService<IAnalyzerService>(provider);

        return client ?? throw new NotSupportedException($"Git provider '{provider}' is not supported");
    }
}
