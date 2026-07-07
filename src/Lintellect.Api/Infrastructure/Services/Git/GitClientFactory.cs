using System.Collections.Concurrent;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Services.Git.AzureDevops;
using Lintellect.Api.Infrastructure.Services.Git.GitHub;
using Lintellect.Shared.Models;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.Infrastructure.Services.Git;

/// <summary>
/// Factory for creating Git clients using credentials resolved at runtime.
/// </summary>
public sealed class GitClientFactory(
    ILogger<GitHubClientService> logger,
    IOptionsMonitor<GitCredentialsOptions> credentialOptions) : IGitClientFactory
{
    private readonly ILogger<GitHubClientService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOptionsMonitor<GitCredentialsOptions> _credentialOptions = credentialOptions ?? throw new ArgumentNullException(nameof(credentialOptions));
    private readonly ConcurrentDictionary<string, IGitClient> _clientCache = new();
    public IGitClient CreateClient(AnalysisRequest analysisRequest)
    {
        return analysisRequest.GitProvider switch
        {
            EGitProvider.AzureDevops => CreateAzureDevOpsClient(),
            EGitProvider.GitHub => CreateGitHubClient(),
            _ => throw new NotSupportedException($"Git provider '{analysisRequest.GitProvider}' is not supported")
        };
    }


    private AzureDevopsClientService CreateAzureDevOpsClient()
    {
        var azureDevOpsOptions = GetCurrentCredentials().AzureDevOps ?? new GitCredentialsOptions.AzureDevOpsCredentials();
        var token = azureDevOpsOptions.Pat;
        var orgUrl = azureDevOpsOptions.OrgUrl;
        _ = Uri.TryCreate(orgUrl, UriKind.Absolute, out var orgUri);

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(orgUrl) || orgUri is null)
        {
            throw new InvalidOperationException("Azure DevOps credentials are not configured. Populate GitCredentials:AzureDevOps in configuration.");
        }

        _logger.LogInformation("Creating Azure DevOps client for organization: {OrgUrl}", orgUri);

        var client = _clientCache.GetOrAdd(token!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new AzureDevopsClientService(token!, orgUri, _logger);
        });

        return (AzureDevopsClientService)client;
    }

    private GitHubClientService CreateGitHubClient()
    {
        var token = GetCurrentCredentials().GitHub?.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("GitHub token is not configured. Populate GitCredentials:GitHub in configuration.");
        }

        _logger.LogInformation("Creating GitHub client");
        var client = _clientCache.GetOrAdd(token!, _ =>
        {
            _logger.LogInformation("Caching Azure DevOps client for PAT.");
            return new GitHubClientService(token!, _logger);
        });

        return (GitHubClientService)client;
    }

    private GitCredentialsOptions GetCurrentCredentials()
    {
        return _credentialOptions.CurrentValue ?? new GitCredentialsOptions();
    }
}
