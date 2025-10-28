using System.Text.RegularExpressions;

namespace Lintellect.Shared.Extensions;

/// <summary>
/// Utility for matching file paths against exclusion patterns.
/// </summary>
public static class FilePatternMatcher
{
    /// <summary>
    /// Checks if a file path matches any of the exclusion patterns.
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="exclusionPatterns">List of exclusion patterns</param>
    /// <returns>True if the file should be excluded, false otherwise</returns>
    public static bool ShouldExclude(string filePath, List<string>? exclusionPatterns)
    {
        if (exclusionPatterns == null || exclusionPatterns.Count == 0)
        {
            return false;
        }

        foreach (var pattern in exclusionPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            if (MatchesPattern(filePath, pattern.Trim()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a file path matches any of the exclusion patterns (legacy string support).
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="exclusionPatterns">Comma-separated list of exclusion patterns</param>
    /// <returns>True if the file should be excluded, false otherwise</returns>
    public static bool ShouldExclude(string filePath, string? exclusionPatterns)
    {
        if (string.IsNullOrWhiteSpace(exclusionPatterns))
        {
            return false;
        }

        var patterns = exclusionPatterns.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return ShouldExclude(filePath, patterns);
    }

    /// <summary>
    /// Checks if a file path matches a specific pattern.
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="pattern">The pattern to match against</param>
    /// <returns>True if the file matches the pattern, false otherwise</returns>
    private static bool MatchesPattern(string filePath, string pattern)
    {
        // Normalize path separators
        var normalizedPath = filePath.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');

        // Handle different pattern types
        if (normalizedPattern.StartsWith("**/"))
        {
            // Recursive pattern: **/pattern
            var subPattern = normalizedPattern[3..];
            return MatchesRecursivePattern(normalizedPath, subPattern);
        }
        else if (normalizedPattern.EndsWith("/**"))
        {
            // Directory pattern: directory/**
            var dirPattern = normalizedPattern[..^3];
            return normalizedPath.StartsWith(dirPattern + "/") || normalizedPath == dirPattern;
        }
        else if (normalizedPattern.Contains("**"))
        {
            // Complex recursive pattern: dir/**/pattern
            return MatchesComplexRecursivePattern(normalizedPath, normalizedPattern);
        }
        else if (normalizedPattern.Contains("*"))
        {
            // Simple wildcard pattern
            return MatchesWildcardPattern(normalizedPath, normalizedPattern);
        }
        else
        {
            // Exact match or directory match
            return normalizedPath == normalizedPattern ||
                   normalizedPath.StartsWith(normalizedPattern + "/");
        }
    }

    /// <summary>
    /// Matches recursive patterns like **/pattern.
    /// </summary>
    private static bool MatchesRecursivePattern(string filePath, string pattern)
    {
        var parts = pattern.Split('/');
        var lastPart = parts[^1];
        var dirParts = parts[..^1];

        // Check if any directory in the path matches the pattern
        var pathParts = filePath.Split('/');
        for (var i = 0; i <= pathParts.Length - parts.Length; i++)
        {
            var matches = true;
            for (var j = 0; j < dirParts.Length; j++)
            {
                if (!MatchesWildcardPattern(pathParts[i + j], dirParts[j]))
                {
                    matches = false;
                    break;
                }
            }
            if (matches && i + dirParts.Length < pathParts.Length)
            {
                var fileName = pathParts[i + dirParts.Length];
                if (MatchesWildcardPattern(fileName, lastPart))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Matches complex recursive patterns like dir/**/pattern.
    /// </summary>
    private static bool MatchesComplexRecursivePattern(string filePath, string pattern)
    {
        // Convert pattern to regex
        var regexPattern = pattern
            .Replace("**", "___RECURSIVE___")
            .Replace("*", "[^/]*")
            .Replace("___RECURSIVE___", ".*");

        return Regex.IsMatch(filePath, $"^{regexPattern}$");
    }

    /// <summary>
    /// Matches simple wildcard patterns.
    /// </summary>
    private static bool MatchesWildcardPattern(string input, string pattern)
    {
        if (pattern == "*")
        {
            return true;
        }

        if (!pattern.Contains("*"))
        {
            return input == pattern;
        }

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(input, regexPattern);
    }

    /// <summary>
    /// Filters a list of file paths based on exclusion patterns.
    /// </summary>
    /// <param name="filePaths">The file paths to filter</param>
    /// <param name="exclusionPatterns">List of exclusion patterns</param>
    /// <returns>Filtered list of file paths</returns>
    public static IEnumerable<string> FilterFiles(IEnumerable<string> filePaths, List<string>? exclusionPatterns)
    {
        return exclusionPatterns == null || exclusionPatterns.Count == 0
            ? filePaths
            : filePaths.Where(filePath => !ShouldExclude(filePath, exclusionPatterns));
    }

    /// <summary>
    /// Filters a list of file paths based on exclusion patterns (legacy string support).
    /// </summary>
    /// <param name="filePaths">The file paths to filter</param>
    /// <param name="exclusionPatterns">Comma-separated list of exclusion patterns</param>
    /// <returns>Filtered list of file paths</returns>
    public static IEnumerable<string> FilterFiles(IEnumerable<string> filePaths, string? exclusionPatterns)
    {
        return string.IsNullOrWhiteSpace(exclusionPatterns)
            ? filePaths
            : filePaths.Where(filePath => !ShouldExclude(filePath, exclusionPatterns));
    }

    /// <summary>
    /// Checks if an absolute file path ends with a relative path pattern.
    /// This is useful for matching absolute paths from analyzers with relative paths from Git diffs.
    /// </summary>
    /// <param name="absolutePath">The absolute file path (e.g., C:\repo\src\MyFile.cs)</param>
    /// <param name="relativePath">The relative file path (e.g., src/MyFile.cs)</param>
    /// <returns>True if the absolute path ends with the relative path, false otherwise</returns>
    public static bool PathEndsWithRelative(string absolutePath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        // Normalize path separators to forward slashes
        var normalizedAbsolute = absolutePath.Replace('\\', '/');
        var normalizedRelative = relativePath.Replace('\\', '/').TrimStart('/');

        // Ensure we're matching at path boundaries to avoid partial matches
        // For example: "src/Utils/Helper.cs" should not match "tests/src/Utils/Helper.cs"
        if (normalizedAbsolute.EndsWith("/" + normalizedRelative, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Handle case where the relative path is the entire absolute path (rare but possible)
        return normalizedAbsolute.Equals(normalizedRelative, StringComparison.OrdinalIgnoreCase);
    }
}
