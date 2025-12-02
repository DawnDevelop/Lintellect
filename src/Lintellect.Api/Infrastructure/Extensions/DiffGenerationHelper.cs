using DiffPlex.Renderer;

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
    public static string GenerateUnifiedDiff(string? originalContent, string? modifiedContent, int contextLines = 20)
    {
        var diffModel = UnidiffRenderer.GenerateUnidiff(originalContent, modifiedContent, contextLines: contextLines);
        return diffModel;
    }
}
