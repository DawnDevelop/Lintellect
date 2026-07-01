namespace Lintellect.Api.Application.Models;


/// <summary>
/// Configuration options for PR analysis. Bound from the "Analysis" configuration section.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>
    /// Number of context lines around changes in the compact diff used for batched analysis
    /// and inline suggestions. Kept tight to bound token usage.
    /// </summary>
    public int ContextLines { get; set; } = 3;

    /// <summary>
    /// Number of context lines around changes in the wider diff used for the narrative
    /// summary and detailed-analysis passes, where more surrounding context helps.
    /// </summary>
    public int DetailedContextLines { get; set; } = 20;

    /// <summary>
    /// Maximum lines to show for new/deleted files.
    /// </summary>
    public int MaxNewFileLines { get; set; } = 50;

    /// <summary>
    /// Maximum total lines per file diff.
    /// </summary>
    public int MaxLinesPerFile { get; set; } = 1000;

    public bool IncludeSummary { get; set; } = true;

    public bool IncludeComprehensiveComment { get; set; } = true;

    public bool IncludeInlineSuggestions { get; set; } = true;

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
