using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Infrastructure.Services.Git;

internal sealed class GitClientResolver(IServiceProvider serviceProvider) : IGitClientResolver
{
    public IGitClient GetClient(EGitProvider provider)
    {
        var client = serviceProvider.GetKeyedService<IGitClient>(provider);

        return client ?? throw provider switch
        {
            EGitProvider.GitHub => new NotImplementedException("GitHub client not yet implemented"),
            _ => new NotSupportedException($"Git provider '{provider}' is not supported")
        };
    }

    public IGitClient GetClient(AnalysisRequest analysisResult)
    {
        return GetClient(analysisResult.GitProvider);
    }
}
