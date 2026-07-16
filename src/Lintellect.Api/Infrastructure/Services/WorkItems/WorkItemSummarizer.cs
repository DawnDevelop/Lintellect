using System.Text;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Infrastructure.Services.AI.Prompts;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.WorkItems;

internal sealed class WorkItemSummarizer(IAnalyzerService analyzerService, ILogger<WorkItemSummarizer> logger) : IWorkItemSummarizer
{
    private const int MaxOutputTokens = 800;
    private const int MaxBodyChars = 4000;

    private readonly PromptTemplateService _templates = new();

    public async Task<WorkItemSummary> SummarizeAsync(IReadOnlyList<WorkItemReference> workItems, CancellationToken cancellationToken = default)
    {
        if (workItems is null || workItems.Count == 0)
        {
            return WorkItemSummary.Empty;
        }

        var systemPrompt = _templates.RenderTemplate(
            AvailablePrompts.GeneralPrompts[GeneralPromptTemplates.WorkItemSummarizerSystemPrompt]);
        var userPrompt = BuildUserPrompt(workItems);

        var response = await analyzerService
            .SummarizeContextAsync(systemPrompt, userPrompt, MaxOutputTokens, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response))
        {
            logger.LogWarning("Work item summarizer returned an empty response for {Count} item(s)", workItems.Count);
            return WorkItemSummary.Empty;
        }

        var (goal, _) = SplitGoalAndContext(response);
        logger.LogInformation("Work item summary generated for {Count} item(s). GoalLength={GoalLength}, ContextLength={ContextLength}",
            workItems.Count, goal.Length, response.Length);
        return new WorkItemSummary(FullContext: response.Trim(), Goal: goal);
    }

    private static string BuildUserPrompt(IReadOnlyList<WorkItemReference> workItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"There are {workItems.Count} linked work item(s). Treat each as a distinct item.");
        builder.AppendLine();

        for (var i = 0; i < workItems.Count; i++)
        {
            var item = workItems[i];
            builder.AppendLine($"--- Work Item {i + 1} of {workItems.Count} ---");
            builder.AppendLine($"Id: {item.Id}");
            if (!string.IsNullOrWhiteSpace(item.Type))
            {
                builder.AppendLine($"Type: {item.Type}");
            }
            if (!string.IsNullOrWhiteSpace(item.State))
            {
                builder.AppendLine($"State: {item.State}");
            }
            if (!string.IsNullOrWhiteSpace(item.Title))
            {
                builder.AppendLine($"Title: {item.Title}");
            }
            if (!string.IsNullOrWhiteSpace(item.Body))
            {
                builder.AppendLine("Body:");
                builder.AppendLine(Truncate(item.Body, MaxBodyChars));
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "... (truncated)";
    }

    internal static (string Goal, string Context) SplitGoalAndContext(string response)
    {
        var lines = response.Split('\n');
        string? goalLine = null;
        var contextStartIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (goalLine is null && trimmed.StartsWith("GOAL:", StringComparison.OrdinalIgnoreCase))
            {
                goalLine = trimmed["GOAL:".Length..].Trim();
            }
            else if (trimmed.StartsWith("CONTEXT:", StringComparison.OrdinalIgnoreCase))
            {
                contextStartIndex = i + 1;
                break;
            }
        }

        var contextText = contextStartIndex >= 0 && contextStartIndex < lines.Length
            ? string.Join('\n', lines[contextStartIndex..]).Trim()
            : string.Empty;

        return (goalLine ?? string.Empty, contextText);
    }
}
