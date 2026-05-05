using Lintellect.Api.Application.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.WorkItems;

internal sealed class WorkItemService(IGitClientFactory gitClientFactory, ILogger<WorkItemService> logger) : IWorkItemService
{
    public async Task<List<WorkItemReference>> ResolveAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisRequest);

        if (analysisRequest.GitInfo is null)
        {
            return [];
        }

        var client = gitClientFactory.CreateClient(analysisRequest);
        var hints = analysisRequest.WorkItems is { Count: > 0 } ? analysisRequest.WorkItems : null;

        var items = await client.GetLinkedWorkItemsAsync(
            analysisRequest.GitInfo.ProjectName ?? analysisRequest.GitInfo.RepositoryName,
            analysisRequest.GitInfo.RepositoryName,
            analysisRequest.GitInfo.PullRequestId,
            hints).ConfigureAwait(false);

        logger.LogInformation("Resolved {Count} linked work item(s) for PR #{PullRequestId}",
            items.Count, analysisRequest.GitInfo.PullRequestId);

        return items;
    }
}
