using System.Text;

namespace Lintellect.Api.Infrastructure.Extensions;

/// <summary>
/// Helper class for generating unified diff output from file contents.
/// Provides both full and compact diff formats optimized for AI token usage.
/// </summary>
public static class DiffGenerationHelper
{
    /// <summary>
    /// Generates a standard unified diff format string from two file contents.
    /// Shows all changes with full context and line numbers.
    /// </summary>
    /// <param name="filePath">The file path for header information.</param>
    /// <param name="originalContent">The original file content (can be null for new files).</param>
    /// <param name="modifiedContent">The modified file content (can be null for deleted files).</param>
    /// <returns>A unified diff format string with line numbers.</returns>
    public static string GenerateUnifiedDiff(string filePath, string? originalContent, string? modifiedContent)
    {
        var diff = new StringBuilder();
        
        diff.AppendLine($"--- a{filePath}");
        diff.AppendLine($"+++ b{filePath}");

        // Handle new file
        if (originalContent is null && modifiedContent is not null)
        {
            var newLines = modifiedContent.Split('\n');
            diff.AppendLine($"@@ -0,0 +1,{newLines.Length} @@");
            for (int i = 0; i < newLines.Length; i++)
            {
                diff.AppendLine($"+{i + 1}:{newLines[i].TrimEnd('\r')}");
            }
            return diff.ToString();
        }

        // Handle deleted file
        if (originalContent is not null && modifiedContent is null)
        {
            var oldLines = originalContent.Split('\n');
            diff.AppendLine($"@@ -1,{oldLines.Length} +0,0 @@");
            for (int i = 0; i < oldLines.Length; i++)
            {
                diff.AppendLine($"-{i + 1}:{oldLines[i].TrimEnd('\r')}");
            }
            return diff.ToString();
        }

        // Handle modified file
        if (originalContent is not null && modifiedContent is not null)
        {
            var oldLines = originalContent.Split('\n');
            var newLines = modifiedContent.Split('\n');
            
            var changes = BuildLineChanges(oldLines, newLines);

            if (changes.Count > 0)
            {
                diff.AppendLine($"@@ -1,{oldLines.Length} +1,{newLines.Length} @@");
                foreach (var change in changes)
                {
                    diff.AppendLine(change);
                }
            }
        }

        return diff.ToString();
    }

    /// <summary>
    /// Generates a compact unified diff showing only changed regions with limited context.
    /// Includes line numbers for accurate code review.
    /// Optimized for AI token usage by limiting output size.
    /// </summary>
    /// <param name="filePath">The file path for header information.</param>
    /// <param name="originalContent">The original file content (can be null for new files).</param>
    /// <param name="modifiedContent">The modified file content (can be null for deleted files).</param>
    /// <param name="contextLines">Number of context lines around each change (default: 3).</param>
    /// <param name="maxNewFileLines">Maximum lines to show for new/deleted files (default: 50).</param>
    /// <param name="maxLinesPerFile">Maximum total lines per file diff (default: 1000).</param>
    /// <returns>A compact unified diff string with line numbers, or null if no changes detected.</returns>
    public static string? GenerateCompactDiff(
        string filePath, 
        string? originalContent, 
        string? modifiedContent, 
        int contextLines = 3,
        int maxNewFileLines = 50,
        int maxLinesPerFile = 1000)
    {
        var diff = new StringBuilder();
        diff.AppendLine($"--- a{filePath}");
        diff.AppendLine($"+++ b{filePath}");

        // Handle new file (limit to first N lines if too large)
        if (originalContent is null && modifiedContent is not null)
        {
            var newLines = modifiedContent.Split('\n');
            var linesToShow = Math.Min(newLines.Length, maxNewFileLines);
            diff.AppendLine($"@@ -0,0 +1,{linesToShow} @@ (New file, showing first {linesToShow} of {newLines.Length} lines)");
            for (int i = 0; i < linesToShow; i++)
            {
                diff.AppendLine($"+{i + 1}:{newLines[i].TrimEnd('\r')}");
            }
            if (newLines.Length > linesToShow)
            {
                diff.AppendLine($"... ({newLines.Length - linesToShow} more lines omitted for token optimization)");
            }
            return diff.ToString();
        }

        // Handle deleted file (limit to first N lines if too large)
        if (originalContent is not null && modifiedContent is null)
        {
            var oldLines = originalContent.Split('\n');
            var linesToShow = Math.Min(oldLines.Length, maxNewFileLines);
            diff.AppendLine($"@@ -1,{linesToShow} +0,0 @@ (Deleted file, showing first {linesToShow} of {oldLines.Length} lines)");
            for (int i = 0; i < linesToShow; i++)
            {
                diff.AppendLine($"-{i + 1}:{oldLines[i].TrimEnd('\r')}");
            }
            if (oldLines.Length > linesToShow)
            {
                diff.AppendLine($"... ({oldLines.Length - linesToShow} more lines omitted for token optimization)");
            }
            return diff.ToString();
        }

        // Handle modified file - extract only changed hunks with line numbers
        if (originalContent is not null && modifiedContent is not null)
        {
            var oldLines = originalContent.Split('\n');
            var newLines = modifiedContent.Split('\n');
            
            // Check if file is too large and needs truncation
            var totalLines = Math.Max(oldLines.Length, newLines.Length);
            if (totalLines > maxLinesPerFile * 2) // If file is extremely large
            {
                diff.AppendLine($"@@ File too large ({totalLines} lines) - showing summary only @@");
                diff.AppendLine($"File has {oldLines.Length} lines in base version");
                diff.AppendLine($"File has {newLines.Length} lines in target version");
                diff.AppendLine($"... (Full diff omitted for token optimization - file exceeds {maxLinesPerFile * 2} line threshold)");
                return diff.ToString();
            }
            
            var hunks = ExtractChangedHunksWithLineNumbers(oldLines, newLines, contextLines);
            
            if (hunks.Count == 0)
                return null; // No changes detected

            var totalDiffLines = 0;
            foreach (var hunk in hunks)
            {
                var hunkLines = hunk.Split('\n').Length;
                if (totalDiffLines + hunkLines > maxLinesPerFile)
                {
                    diff.AppendLine($"... ({hunks.Count - hunks.IndexOf(hunk)} more hunks omitted - exceeds {maxLinesPerFile} line limit)");
                    break;
                }
                diff.AppendLine(hunk);
                totalDiffLines += hunkLines;
            }
        }

        return diff.ToString();
    }

    /// <summary>
    /// Builds line-by-line changes with line numbers for modified files.
    /// </summary>
    private static List<string> BuildLineChanges(string[] oldLines, string[] newLines)
    {
        var changes = new List<string>();
        var maxLines = Math.Max(oldLines.Length, newLines.Length);

        int oldLineNum = 1;
        int newLineNum = 1;

        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i].TrimEnd('\r') : null;
            var newLine = i < newLines.Length ? newLines[i].TrimEnd('\r') : null;

            if (oldLine != newLine)
            {
                if (oldLine is not null)
                {
                    changes.Add($"-{oldLineNum}:{oldLine}");
                    oldLineNum++;
                }
                if (newLine is not null)
                {
                    changes.Add($"+{newLineNum}:{newLine}");
                    newLineNum++;
                }
            }
            else if (oldLine is not null)
            {
                changes.Add($" {oldLineNum}:{oldLine}");
                oldLineNum++;
                newLineNum++;
            }
        }

        return changes;
    }

    /// <summary>
    /// Extracts changed hunks from file comparison with surrounding context and line numbers.
    /// Each hunk shows only the changed region plus N context lines before/after.
    /// Line numbers are included to help AI generate accurate inline suggestions.
    /// </summary>
    /// <param name="oldLines">Lines from the original file.</param>
    /// <param name="newLines">Lines from the modified file.</param>
    /// <param name="contextLines">Number of context lines to include around changes.</param>
    /// <returns>List of diff hunks in unified diff format with line numbers.</returns>
    public static List<string> ExtractChangedHunksWithLineNumbers(string[] oldLines, string[] newLines, int contextLines)
    {
        var hunks = new List<string>();
        var changes = new List<(int oldLineNum, int newLineNum, string type, string line)>(); // type: "old", "new", "same"
        
        // Line-by-line comparison with proper line number tracking
        int oldIdx = 0;
        int newIdx = 0;
        
        while (oldIdx < oldLines.Length || newIdx < newLines.Length)
        {
            var oldLine = oldIdx < oldLines.Length ? oldLines[oldIdx].TrimEnd('\r') : null;
            var newLine = newIdx < newLines.Length ? newLines[newIdx].TrimEnd('\r') : null;

            if (oldLine == newLine && oldLine is not null)
            {
                changes.Add((oldIdx + 1, newIdx + 1, "same", oldLine));
                oldIdx++;
                newIdx++;
            }
            else
            {
                // Lines differ - add both as changes
                if (oldLine is not null)
                {
                    changes.Add((oldIdx + 1, -1, "old", oldLine));
                    oldIdx++;
                }
                if (newLine is not null)
                {
                    changes.Add((-1, newIdx + 1, "new", newLine));
                    newIdx++;
                }
            }
        }

        // Find continuous regions of changes
        var changeRegions = new List<(int start, int end)>();
        int? regionStart = null;
        
        for (int i = 0; i < changes.Count; i++)
        {
            if (changes[i].type != "same")
            {
                regionStart ??= i;
            }
            else if (regionStart.HasValue)
            {
                changeRegions.Add((regionStart.Value, i - 1));
                regionStart = null;
            }
        }
        if (regionStart.HasValue)
        {
            changeRegions.Add((regionStart.Value, changes.Count - 1));
        }

        // Build hunks with context and line numbers
        foreach (var (start, end) in changeRegions)
        {
            var hunkStart = Math.Max(0, start - contextLines);
            var hunkEnd = Math.Min(changes.Count - 1, end + contextLines);
            
            var hunk = new StringBuilder();
            
            // Calculate hunk header info
            var firstOldLine = changes[hunkStart].oldLineNum > 0 ? changes[hunkStart].oldLineNum : 1;
            var firstNewLine = changes[hunkStart].newLineNum > 0 ? changes[hunkStart].newLineNum : 1;
            
            var oldCount = changes.Skip(hunkStart).Take(hunkEnd - hunkStart + 1)
                .Count(c => c.type is "old" or "same");
            var newCount = changes.Skip(hunkStart).Take(hunkEnd - hunkStart + 1)
                .Count(c => c.type is "new" or "same");

            hunk.AppendLine($"@@ -{firstOldLine},{oldCount} +{firstNewLine},{newCount} @@");
            
            // Add hunk lines with line numbers
            for (int i = hunkStart; i <= hunkEnd; i++)
            {
                var (oldLineNum, newLineNum, type, line) = changes[i];
                var prefix = type switch
                {
                    "old" => $"-{oldLineNum}:",
                    "new" => $"+{newLineNum}:",
                    "same" => $" {newLineNum}:",
                    _ => " "
                };
                hunk.AppendLine($"{prefix}{line}");
            }

            hunks.Add(hunk.ToString());
        }

        return hunks;
    }

    /// <summary>
    /// Legacy method maintained for backward compatibility.
    /// Extracts changed hunks without line numbers.
    /// </summary>
    /// <param name="oldLines">Lines from the original file.</param>
    /// <param name="newLines">Lines from the modified file.</param>
    /// <param name="contextLines">Number of context lines to include around changes.</param>
    /// <returns>List of diff hunks in unified diff format.</returns>
    public static List<string> ExtractChangedHunks(string[] oldLines, string[] newLines, int contextLines)
    {
        // Use the new method but strip line numbers for backward compatibility
        var hunksWithNumbers = ExtractChangedHunksWithLineNumbers(oldLines, newLines, contextLines);
        var hunks = new List<string>();
        
        foreach (var hunk in hunksWithNumbers)
        {
            var lines = hunk.Split('\n');
            var result = new StringBuilder();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    result.AppendLine(line);
                }
                else if (line.StartsWith("-") || line.StartsWith("+") || line.StartsWith(" "))
                {
                    // Remove line number (everything between prefix and first colon)
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var prefix = line[0].ToString();
                        var content = line[(colonIndex + 1)..];
                        result.AppendLine($"{prefix}{content}");
                    }
                    else
                    {
                        result.AppendLine(line);
                    }
                }
                else
                {
                    result.AppendLine(line);
                }
            }
            
            hunks.Add(result.ToString());
        }
        
        return hunks;
    }
}
