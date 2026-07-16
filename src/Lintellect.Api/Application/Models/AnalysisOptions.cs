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
    public int ContextLines { get; set; } = 20;

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

    /// <summary>
    /// When true, runs the analysis passes as direct parallel API calls instead of the Claude
    /// batch endpoint. Trades the ~50% batch token discount for bounded latency — the batch tier
    /// has no completion-time guarantee and can exceed the job timeout under load.
    /// Set via env: Analysis__SynchronousAnalysis.
    /// </summary>
    public bool SynchronousAnalysis { get; set; } = false;

    /// <summary>
    /// Azure DevOps work item fields composed (in order, each labeled) into the work-item body
    /// fed to the AI. Fields absent on a given work item type are skipped, so the defaults cover
    /// stories/PBIs (acceptance criteria) and bugs (repro steps) across the standard process
    /// templates; override for custom processes. Set via env: Analysis__WorkItemBodyFields__0 etc.
    /// </summary>
    public List<string> WorkItemBodyFields { get; set; } =
    [
        "System.Description",
        "Microsoft.VSTS.Common.AcceptanceCriteria",
        "Microsoft.VSTS.TCM.ReproSteps"
    ];

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
