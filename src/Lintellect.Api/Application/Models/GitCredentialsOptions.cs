namespace Lintellect.Api.Application.Models;

/// <summary>
/// Configuration options for self-hosted Git provider credentials.
/// Intended for single-tenant deployments.
/// </summary>
public sealed class GitCredentialsOptions
{
    public AzureDevOpsCredentials AzureDevOps { get; set; } = new();
    public GitHubCredentials GitHub { get; set; } = new();

    public sealed class AzureDevOpsCredentials
    {
        public string? OrgUrl { get; set; }
        public string? Pat { get; set; }
    }

    public sealed class GitHubCredentials
    {
        public string? Token { get; set; }
    }
}


