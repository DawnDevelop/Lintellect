using System.Text;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Services;

/// <summary>
/// Formats linked work items into prompt-ready context. The full block (title, state and body per
/// item) goes into the Summary and Detailed-Analysis prompts; the per-file Inline-Suggestion
/// prompts only get the one-line title list to keep the per-file token cost bounded.
/// </summary>
internal static class WorkItemPromptFormatter
{
    private const int MaxBodyChars = 4000;

    public static string ToPromptBlock(IReadOnlyList<WorkItemReference> workItems)
    {
        if (workItems is not { Count: > 0 })
        {
            return string.Empty;
        }

        var items = string.Join("\n\n", workItems.Select(FormatItem));
        return $"""
            ## Linked Work Items (the intent of this PR)

            {items}

            Evaluate whether the changes fulfill the intent of the linked work items. Explicitly call out anything from their scope that appears missing or contradicted by the diff.
            """;
    }

    public static string ToGoalPromptLine(IReadOnlyList<WorkItemReference> workItems)
    {
        if (workItems is not { Count: > 0 })
        {
            return string.Empty;
        }

        var titles = string.Join("; ", workItems
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => item.Title!.Trim()));

        return string.IsNullOrWhiteSpace(titles)
            ? string.Empty
            : $"The intent of this PR (from its linked work items): {titles}";
    }

    private static string FormatItem(WorkItemReference item)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"### {item.Id}: {item.Title}");
        if (!string.IsNullOrWhiteSpace(item.Type))
        {
            builder.AppendLine($"Type: {item.Type}");
        }
        if (!string.IsNullOrWhiteSpace(item.State))
        {
            builder.AppendLine($"State: {item.State}");
        }
        if (!string.IsNullOrWhiteSpace(item.Body))
        {
            builder.AppendLine();
            builder.AppendLine(Truncate(item.Body, MaxBodyChars));
        }

        return builder.ToString().TrimEnd();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "... (truncated)";
    }
}
