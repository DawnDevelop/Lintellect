using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace devops_pr_analyzer.Services;

/// <summary>
/// Provides functionality for connecting to an Azure DevOps organization using a personal access token (PAT).
/// </summary>
/// <remarks>Use this service to establish authenticated connections to Azure DevOps REST APIs. The connection is
/// initialized with the provided organization URL and PAT, enabling access to organization-level resources.</remarks>
/// <param name="devopsPat">The personal access token used to authenticate requests to Azure DevOps. Cannot be null or empty.</param>
/// <param name="orgUri">The base URL of the Azure DevOps organization. (https://dev.azure.com/orgname) </param>
public class AzureDevopsClientService(string devopsPat, Uri orgUri)
{
    private readonly VssConnection _connection = new(orgUri, new VssOAuthAccessTokenCredential(devopsPat));

    public Task<GitHttpClient> GetGitClient() 
        => _connection.GetClientAsync<GitHttpClient>();

    public Task<ProjectHttpClient> GetProjectClient() 
        => _connection.GetClientAsync<ProjectHttpClient>();
}
