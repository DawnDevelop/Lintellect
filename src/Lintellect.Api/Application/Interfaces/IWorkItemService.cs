using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Resolves work items / issues linked to a pull request, regardless of provider.
/// Wraps <see cref="IGitClientFactory"/> + <see cref="IGitClient.GetLinkedWorkItemsAsync"/> so the
/// orchestrator can stay provider-agnostic.
/// </summary>
public interface IWorkItemService
{
    Task<List<WorkItemReference>> ResolveAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken = default);
}
