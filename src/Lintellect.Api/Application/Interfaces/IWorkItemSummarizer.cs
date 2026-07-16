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

    /// <summary>
    /// Renders the full GOAL + CONTEXT block framed for prompt injection. Returns an empty string
    /// when there is no context so the rendered prompt contains no dangling heading.
    /// </summary>
    public string ToPromptBlock()
    {
        if (string.IsNullOrWhiteSpace(FullContext))
        {
            return string.Empty;
        }

        return $"""
            ## Linked Work Item (the intent of this PR)

            {FullContext}

            Evaluate whether the changes fulfill this GOAL. Explicitly call out anything from the work item's scope that appears missing or contradicted by the diff.
            """;
    }

    /// <summary>
    /// Renders the single GOAL line framed for the per-file inline prompts, or an empty string.
    /// </summary>
    public string ToGoalPromptLine()
    {
        return string.IsNullOrWhiteSpace(Goal)
            ? string.Empty
            : $"The intent of this PR (from its linked work item): {Goal}";
    }
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
