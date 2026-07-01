using Lintellect.Api.Application.Models;

namespace Lintellect.Api.Application.Services;

/// <summary>
/// Stateless policy for bounding the number of inline suggestions posted to a PR.
/// Provider-agnostic — shared by all analyzer services so the capping behavior stays consistent.
/// </summary>
internal static class InlineSuggestionLimiter
{
    /// <summary>
    /// Applies a global cap to inline suggestions, selecting the highest-severity ones first.
    /// </summary>
    public static List<InlineSuggestion> ApplyGlobalCap(List<InlineSuggestion> suggestions, int maxInlineSuggestions, ILogger? logger = null)
    {
        if (maxInlineSuggestions <= 0 || suggestions.Count <= maxInlineSuggestions)
        {
            return suggestions;
        }

        var capped = suggestions
            .OrderByDescending(s => s.Severity?.ToLowerInvariant() switch
            {
                "error" => 3,
                "warning" => 2,
                "info" => 1,
                _ => 0
            })
            .Take(maxInlineSuggestions)
            .ToList();

        logger?.LogInformation(
            "Inline suggestions capped from {Original} to {Capped} (MaxInlineSuggestions={Max})",
            suggestions.Count, capped.Count, maxInlineSuggestions);

        return capped;
    }

    /// <summary>
    /// Computes the per-file suggestion limit based on PR size and global cap.
    /// For small PRs the budget is generous; for large PRs it tightens automatically.
    /// </summary>
    public static int ComputeMaxSuggestionsPerFile(int fileCount, int globalMax)
    {
        if (fileCount <= 0) return 5;
        var perFile = globalMax > 0 ? Math.Max(1, globalMax / fileCount) : 5;
        return Math.Min(perFile, 5); // never exceed 5 per file even for very small PRs
    }
}
