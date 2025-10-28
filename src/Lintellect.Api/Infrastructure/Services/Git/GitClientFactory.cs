using System.Collections.Concurrent;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Infrastructure.Services.Git.AzureDevops;
using Lintellect.Api.Infrastructure.Services.Git.GitHub;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.Git;

/// <summary>
/// Factory for creating Git clients with dynamic credentials from AnalysisRequest.
/// </summary>
public sealed class GitClientFactory(ILogger<GitHubClientService> logger) : IGitClientFactory
{
    private readonly ILogger<GitHubClientService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<string, IGitClient> _clientCache = new();
    public IGitClient CreateClient(AnalysisRequest analysisRequest)
    {
        return analysisRequest.GitProvider switch
        {
            EGitProvider.AzureDevops => CreateAzureDevOpsClient(analysisRequest),
            EGitProvider.GitHub => CreateGitHubClient(analysisRequest),
            _ => throw new NotSupportedException($"Git provider '{analysisRequest.GitProvider}' is not supported")
        };
    }

    private AzureDevopsClientService CreateAzureDevOpsClient(AnalysisRequest analysisRequest)
    {
        // Validation is handled by FluentValidation at the request level
        var orgUri = new Uri(analysisRequest.AzureDevOpsOrgUrl!);

        _logger.LogInformation("Creating Azure DevOps client for organization: {OrgUrl}", orgUri);

        var client = _clientCache.GetOrAdd(analysisRequest.DevopsPat!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new AzureDevopsClientService(analysisRequest.DevopsPat!, orgUri);
        });

        return (AzureDevopsClientService)client;
    }

    private GitHubClientService CreateGitHubClient(AnalysisRequest analysisRequest)
    {
        // Validation is handled by FluentValidation at the request level
        _logger.LogInformation("Creating GitHub client");
        var client = _clientCache.GetOrAdd(analysisRequest.GitHubToken!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new GitHubClientService(analysisRequest.GitHubToken!, _logger);
        });

        return (GitHubClientService)client;
    }
}
