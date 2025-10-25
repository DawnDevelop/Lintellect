namespace devops_pr_analyzer.Application.Models;


/// <summary>
/// Statistics about code changes in the PR.
/// </summary>
public sealed class DiffStatistics
{
    /// <summary>
    /// Number of files changed.
    /// </summary>
    public required int FilesChanged { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public required int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines removed.
    /// </summary>
    public required int LinesRemoved { get; init; }

    /// <summary>
    /// Total lines changed (added + removed).
    /// </summary>
    public int TotalChanges => LinesAdded + LinesRemoved;
}
