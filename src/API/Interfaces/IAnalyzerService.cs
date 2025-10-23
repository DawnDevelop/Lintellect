using devops_pr_analyzer.Models;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Interfaces;

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
    Task<string> AnalyzeAsync(
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON-formatted string containing parsed code ownership information, or null if analysis fails.</returns>
    Task<string?> GetCodeOwnersAsync(string codeOwnerFileContent, CancellationToken cancellationToken = default);
}
