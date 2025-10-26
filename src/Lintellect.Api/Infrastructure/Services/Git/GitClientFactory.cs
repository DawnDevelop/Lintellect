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
        return new AzureDevopsClientService(analysisRequest.DevopsPat!, orgUri);
    }

    private GitHubClientService CreateGitHubClient(AnalysisRequest analysisRequest)
    {
        // Validation is handled by FluentValidation at the request level
        _logger.LogInformation("Creating GitHub client");
        return new GitHubClientService(analysisRequest.GitHubToken!, _logger);
    }
}
