using Lintellect.Api.Application.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI;

internal sealed class AnalyzerServiceResolver(IServiceProvider serviceProvider) : IAnalyzerServiceResolver
{
    public IAnalyzerService GetAnalyzerService(EAnalyzers provider)
    {
        var client = serviceProvider.GetKeyedService<IAnalyzerService>(provider);

        return client ?? throw new NotSupportedException($"Git provider '{provider}' is not supported");
    }
}
