using System.Collections.Concurrent;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Infrastructure.Services.Git.AzureDevops;
using Lintellect.Api.Infrastructure.Services.Git.GitHub;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.Git;

/// <summary>
/// Factory for creating Git clients using credentials resolved at runtime.
/// </summary>
public sealed class GitClientFactory(ILogger<GitHubClientService> logger, ICredentialResolver credentialResolver) : IGitClientFactory
{
    private readonly ILogger<GitHubClientService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICredentialResolver _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
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
        var (token, orgUri) = _credentialResolver.Resolve(analysisRequest);

        if (string.IsNullOrWhiteSpace(token) || orgUri is null)
        {
            throw new InvalidOperationException("Azure DevOps credentials are not configured. Provide DevopsPat/AzureDevOpsOrgUrl in request or set GitCredentials:AzureDevOps in configuration.");
        }

        _logger.LogInformation("Creating Azure DevOps client for organization: {OrgUrl}", orgUri);

        var client = _clientCache.GetOrAdd(token!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new AzureDevopsClientService(token!, orgUri);
        });

        return (AzureDevopsClientService)client;
    }

    private GitHubClientService CreateGitHubClient(AnalysisRequest analysisRequest)
    {
        var (token, _) = _credentialResolver.Resolve(analysisRequest);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("GitHub token is not configured. Provide GitHubToken in request or set GitCredentials:GitHub in configuration.");
        }

        _logger.LogInformation("Creating GitHub client");
        var client = _clientCache.GetOrAdd(token!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new GitHubClientService(token!, _logger);
        });

        return (GitHubClientService)client;
    }
}
