using Lintellect.Shared.Extensions;

namespace Lintellect.Api.Infrastructure.Services.Git;

/// <summary>
/// Pre-filters a CODEOWNERS file down to the lines whose path patterns actually match the
/// pull request's changed files. Skipping the LLM call entirely when there are no matches
/// avoids paying ~890+ tokens for a system prompt that can only ever return an empty result.
/// </summary>
internal static class CodeOwnersPathFilter
{
    /// <summary>
    /// Returns CODEOWNERS lines (preserving the original line text) that match at least one of
    /// <paramref name="changedFilePaths"/>, plus surrounding comments/blank lines that immediately
    /// precede a kept rule for context. Returns an empty string if no rules match.
    /// </summary>
    public static string FilterMatchingLines(string codeOwnersContent, IEnumerable<string> changedFilePaths)
    {
        if (string.IsNullOrWhiteSpace(codeOwnersContent))
        {
            return string.Empty;
        }

        var paths = changedFilePaths?.ToList() ?? [];
        if (paths.Count == 0)
        {
            return string.Empty;
        }

        var lines = codeOwnersContent.Split('\n');
        var kept = new List<string>();
        var pendingComments = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                pendingComments.Add(line);
                continue;
            }

            var pattern = ExtractPattern(trimmed);
            if (pattern is null)
            {
                pendingComments.Clear();
                continue;
            }

            var translated = TranslateGitignorePattern(pattern);
            if (paths.Any(p => FilePatternMatcher.ShouldExclude(p, [translated])))
            {
                kept.AddRange(pendingComments);
                kept.Add(line);
            }

            pendingComments.Clear();
        }

        return kept.Count == 0 ? string.Empty : string.Join('\n', kept);
    }

    /// <summary>
    /// CODEOWNERS lines are `&lt;pattern&gt; &lt;owner&gt; [&lt;owner&gt;...]`. The first whitespace-separated
    /// token is the path pattern.
    /// </summary>
    private static string? ExtractPattern(string line)
    {
        var idx = line.IndexOfAny([' ', '\t']);
        return idx <= 0 ? null : line[..idx];
    }

    /// <summary>
    /// Translates a CODEOWNERS / gitignore-style pattern into the form
    /// <see cref="FilePatternMatcher"/> understands.
    /// </summary>
    private static string TranslateGitignorePattern(string pattern)
    {
        // Leading slash anchors at repo root — strip it (paths in our diffs are repo-relative
        // without a leading slash).
        if (pattern.StartsWith('/'))
        {
            pattern = pattern[1..];
        }

        // Trailing slash means "this directory and everything under it".
        if (pattern.EndsWith('/'))
        {
            pattern = pattern + "**";
        }

        // Bare-name patterns with no slash (e.g. "*.md", "build") match anywhere in the tree.
        if (!pattern.Contains('/'))
        {
            pattern = "**/" + pattern;
        }

        return pattern;
    }
}
