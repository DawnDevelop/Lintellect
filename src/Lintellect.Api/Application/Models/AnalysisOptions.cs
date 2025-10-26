namespace Lintellect.Api.Application.Models;


/// <summary>
/// Configuration options for PR analysis.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>
    /// Number of context lines around changes in diffs.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Maximum lines to show for new/deleted files.
    /// </summary>
    public int MaxNewFileLines { get; init; } = 50;

    /// <summary>
    /// Maximum total lines per file diff.
    /// </summary>
    public int MaxLinesPerFile { get; init; } = 1000;

    public bool IncludeSummary { get; init; } = true;

    public bool IncludeComprehensiveComment { get; init; } = true;

    public bool IncludeInlineSuggestions { get; init; } = true;
    /// <summary>
    /// Default analysis options.
    /// </summary>
    public static AnalysisOptions Default => new();

    /// <summary>
    /// Comprehensive options for detailed analysis.
    /// </summary>
    public static AnalysisOptions Comprehensive => new()
    {
        ContextLines = 80,
        MaxNewFileLines = 1000,
        MaxLinesPerFile = 2000
    };
}

