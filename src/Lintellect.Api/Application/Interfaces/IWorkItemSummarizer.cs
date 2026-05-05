using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Result of summarizing a set of linked work items into a compact AI-ready context block.
/// <see cref="FullContext"/> contains the full GOAL + CONTEXT block injected into Summary and
/// Detailed-Analysis prompts. <see cref="Goal"/> is the single GOAL line injected into the
/// per-file Inline-Suggestion prompts (kept tight to avoid per-file token blow-up).
/// </summary>
public sealed record WorkItemSummary(string FullContext, string Goal)
{
    public static WorkItemSummary Empty { get; } = new(string.Empty, string.Empty);
}

/// <summary>
/// Condenses a set of <see cref="WorkItemReference"/>s into a tight context block suitable for
/// injection into the main code-review prompts. Implementations call the configured
/// <see cref="IAnalyzerService"/> with a hard token cap.
/// </summary>
public interface IWorkItemSummarizer
{
    Task<WorkItemSummary> SummarizeAsync(IReadOnlyList<WorkItemReference> workItems, CancellationToken cancellationToken = default);
}
