using System.Text.Json;
using System.Text.RegularExpressions;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services.Git;

/// <summary>
/// Extracts cheap work-item / issue id hints from CI environment variables.
/// Rich resolution (titles, descriptions) happens API-side because the CLI typically lacks PATs.
/// </summary>
internal static partial class WorkItemReferenceExtractor
{
    [GeneratedRegex(@"\b(?:close[sd]?|fix(?:es|ed)?|resolve[sd]?)\s+#(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LinkedIssueRegex();

    public static IEnumerable<WorkItemReference> ExtractFromEnvironment()
    {
        // GitHub Actions: the PR body lives inside the event payload JSON.
        // Azure DevOps does not surface linked work-item ids through env vars; the API resolves
        // those server-side via the WIT REST API using the configured PAT.
        var eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
        if (string.IsNullOrWhiteSpace(eventPath) || !File.Exists(eventPath))
        {
            yield break;
        }

        string body;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(eventPath));
            if (!doc.RootElement.TryGetProperty("pull_request", out var pr) ||
                !pr.TryGetProperty("body", out var bodyElement) ||
                bodyElement.ValueKind != JsonValueKind.String)
            {
                yield break;
            }

            body = bodyElement.GetString() ?? string.Empty;
        }
        catch (IOException)
        {
            yield break;
        }
        catch (JsonException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var id in ParseLinkedIssueIds(body))
        {
            yield return new WorkItemReference(Id: id.ToString());
        }
    }

    internal static IEnumerable<int> ParseLinkedIssueIds(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            yield break;
        }

        var seen = new HashSet<int>();
        foreach (Match match in LinkedIssueRegex().Matches(body))
        {
            if (int.TryParse(match.Groups[1].Value, out var id) && seen.Add(id))
            {
                yield return id;
            }
        }
    }
}
