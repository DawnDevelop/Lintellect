using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Application.Interfaces;

/// <summary>
/// Factory interface for creating Git clients with dynamic credentials.
/// </summary>
public interface IGitClientFactory
{
  /// <summary>
  /// Creates a Git client based on the provider and credentials in the analysis request.
  /// </summary>
  /// <param name="analysisRequest">The analysis request containing provider and credential information.</param>
  /// <returns>A configured Git client for the specified provider.</returns>
  IGitClient CreateClient(AnalysisRequest analysisRequest);
}
