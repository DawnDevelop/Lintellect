using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.Renderer;

namespace Lintellect.Api.Infrastructure.Extensions;

/// <summary>
/// Helper class for generating unified diff output from file contents.
/// Provides both full and compact diff formats optimized for AI token usage.
/// </summary>
public static partial class DiffGenerationHelper
{
    /// <summary>
    /// Generates a standard unified diff format string from two file contents.
    /// Shows all changes with full context and line numbers.
    /// </summary>
    /// <param name="filePath">The file path for header information.</param>
    /// <param name="originalContent">The original file content (can be null for new files).</param>
    /// <param name="modifiedContent">The modified file content (can be null for deleted files).</param>
    /// <returns>A unified diff format string with line numbers.</returns>
    public static string GenerateUnifiedDiff(string? originalContent = "", string? modifiedContent = "", int contextLines = 3)
    {
        var diffModel = UnidiffRenderer.GenerateUnidiff(originalContent ?? string.Empty, modifiedContent ?? string.Empty, contextLines: contextLines);
        return diffModel;
    }

    /// <summary>
    /// Prefixes each line of a unified diff with its line number in the NEW file, so an AI
    /// reviewer can read the number directly instead of computing it from hunk headers.
    /// Format per line: <c>&lt;new-file line number&gt;|&lt;diff marker&gt;&lt;code&gt;</c>.
    /// Removed lines and hunk/file headers get a blank number (no new-file position).
    /// </summary>
    public static string AnnotateWithLineNumbers(string unifiedDiff)
    {
        if (string.IsNullOrEmpty(unifiedDiff))
        {
            return unifiedDiff;
        }

        const string blank = "      |";
        var lines = unifiedDiff.Split('\n');
        var builder = new StringBuilder(unifiedDiff.Length + lines.Length * 8);
        var newLine = 0;
        var inHunk = false;

        foreach (var line in lines)
        {
            var hunk = HunkHeaderRegex().Match(line);
            if (hunk.Success)
            {
                newLine = int.Parse(hunk.Groups[1].Value);
                inHunk = true;
                builder.Append(blank).Append(line).Append('\n');
                continue;
            }

            // File headers before the first hunk, empty trailing lines, and removed lines
            // have no position in the new file.
            if (!inHunk || line.Length == 0 || line[0] == '-')
            {
                builder.Append(blank).Append(line).Append('\n');
                continue;
            }

            // Added ('+') and context (' ') lines exist in the new file.
            builder.Append($"{newLine,6}|").Append(line).Append('\n');
            newLine++;
        }

        if (builder.Length > 0 && builder[^1] == '\n')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@")]
    private static partial Regex HunkHeaderRegex();
}
