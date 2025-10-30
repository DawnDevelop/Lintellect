using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Shared.Models;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.Infrastructure.Services.Git;

/// <summary>
/// Resolves credentials for Git providers from request overrides or configured defaults.
/// </summary>
public sealed class CredentialResolver(IOptions<GitCredentialsOptions> options, IConfiguration configuration) : ICredentialResolver
{
    private readonly GitCredentialsOptions _options = options?.Value ?? new GitCredentialsOptions();

    public (string? accessToken, Uri? orgUri) Resolve(AnalysisRequest request)
    {
        switch (request.GitProvider)
        {
            case EGitProvider.AzureDevops:
                {
                    var token = FirstNonEmpty(
                              request.AccessToken,
                              _options.AzureDevOps.Pat,
                        configuration.GetValue<string>("AZURE_DEVOPS_PAT"));

                    var orgUrl = FirstNonEmpty(
                              request.AzureDevOpsOrgUrl,
                        _options.AzureDevOps.OrgUrl,
                        configuration.GetValue<string>("AZURE_DEVOPS_ORG_URL"));

                    return (token, string.IsNullOrWhiteSpace(orgUrl) ? null : new Uri(orgUrl!));
                }

            case EGitProvider.GitHub:
                {
                    var token = FirstNonEmpty(
                              request.AccessToken,
                              _options.GitHub.Token,
                        configuration.GetValue<string>("GITHUB_TOKEN"));

                    return (token, null);
                }

            default:
                return (null, null);
        }
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var c in candidates)
        {
            if (!string.IsNullOrWhiteSpace(c))
            {
                return c;
            }
        }

        return null;
    }
}


