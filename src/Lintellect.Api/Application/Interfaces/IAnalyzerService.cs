using Lintellect.Api.Application.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Base interface for AI-powered code analysis services.
/// Provides methods for analyzing code changes and generating insights.
/// </summary>
public interface IAnalyzerService
{
    /// <summary>
    /// Analyzes code findings and diffs using AI to provide insights and recommendations.
    /// Returns a comprehensive, DevOps-ready PR review in Markdown format.
    /// </summary>
    /// <param name="analysisResult">The analysis result containing findings from static analysis</param>
    /// <param name="diffs">Dictionary of file paths to their compact diffs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI-generated detailed analysis with code suggestions in Markdown</returns>
    Task<string> GetDetailedAnalysisAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a concise summary of the pull request suitable for quick reviews and DevOps PR comments.
    /// </summary>
    /// <param name="analysisResult">The analysis result containing findings from static analysis</param>
    /// <param name="diffs">Dictionary of file paths to their compact diffs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Brief PR summary (under 150 words) in Markdown format</returns>
    Task<string> GenerateSummaryAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates structured inline code suggestions that can be posted as individual PR comments.
    /// Each suggestion includes the file path, line number, and corrected code ready to be posted.
    /// </summary>
    /// <param name="analysisResult">The analysis result containing findings with file paths and line numbers</param>
    /// <param name="diffs">Dictionary of file paths to their compact diffs for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of inline suggestions with file path, line number, and suggested fix</returns>
    Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes CODEOWNERS file content and extracts structured code ownership information.
    /// Uses AI to parse and interpret CODEOWNERS file format, returning suggested owners for files.
    /// </summary>
    /// <param name="codeOwnerFileContent">The raw content of the CODEOWNERS file to analyze.</param>
    /// <param name="changedFilePaths">List of file paths that were changed in the pull request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON-formatted string containing parsed code ownership information, or null if analysis fails.</returns>
    Task<CodeOwnersResult?> GetCodeOwnersAsync(string codeOwnerFileContent, List<string> changedFilePaths, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended interface for analyzer services that support batched operations.
/// Follows the Interface Segregation Principle (ISP) - only services with batch capabilities need to implement this.
/// This is a cleaner design than default interface methods, as it makes capabilities explicit.
/// </summary>
public interface IBatchAnalyzerService : IAnalyzerService
{
    /// <summary>
    /// Executes all analysis operations in a single batched request.
    /// This is significantly more efficient than individual calls, reducing latency and costs.
    /// </summary>
    /// <param name="analysisResult">The analysis result containing findings from static analysis</param>
    /// <param name="diffs">Dictionary of file paths to their compact diffs</param>
    /// <param name="codeOwnerFileContent">The raw content of the CODEOWNERS file (if available)</param>
    /// <param name="changedFilePaths">List of file paths that were changed in the pull request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batched results containing all analysis outputs</returns>
    Task<BatchedAnalysisResult> RunBatchedAnalysisAsync(
        AnalyzerServiceModel analysisResult,
        Dictionary<string, string> diffs,
        string? codeOwnerFileContent,
        List<string> changedFilePaths,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Container for batched analysis results from AI services.
/// Used when all analysis operations are executed in a single batch request.
/// </summary>
public sealed class BatchedAnalysisResult
{
    public string DetailedAnalysis { get; set; } = string.Empty;
    public List<InlineSuggestion> InlineSuggestions { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public CodeOwnersResult? CodeOwners { get; set; }
}
