using devops_pr_analyzer.Services;
using devops_pr_analyzer.shared.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace devops_pr_analyzer.Extensions;

/// <summary>
/// Extension methods for Azure DevOps operations related to analysis results.
/// </summary>
public static class AzureDevOpsExtensions
{
  /// <summary>
  /// Retrieves the pull request diff from an AnalysisResult.
  /// </summary>
  /// <param name="client">The Azure DevOps client service.</param>
  /// <param name="analysisResult">The analysis result containing Git information.</param>
  /// <param name="projectName">The Azure DevOps project name.</param>
  /// <returns>The diff of the pull request, or null if the analysis is not for a pull request.</returns>
  /// <exception cref="InvalidOperationException">Thrown when GitInfo is missing or not a pull request type.</exception>
  public static async Task<GitCommitDiffs?> GetPullRequestDiffFromAnalysisAsync(
      this AzureDevopsClientService client,
      AnalysisResult analysisResult,
      string projectName)
  {
    if (analysisResult.GitInfo is null)
    {
      throw new InvalidOperationException("GitInfo is missing from the analysis result.");
    }

    if (analysisResult.GitInfo.Type != GitInfoType.PullRequest)
    {
      return null;
    }

    if (!int.TryParse(analysisResult.GitInfo.Identifier, out var pullRequestId))
    {
      throw new InvalidOperationException($"Invalid pull request identifier: {analysisResult.GitInfo.Identifier}");
    }

    return await client.GetPullRequestDiffAsync(
        projectName,
        analysisResult.GitInfo.RepositoryName,
        pullRequestId)
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Retrieves the pull request details from an AnalysisResult.
  /// </summary>
  /// <param name="client">The Azure DevOps client service.</param>
  /// <param name="analysisResult">The analysis result containing Git information.</param>
  /// <param name="projectName">The Azure DevOps project name.</param>
  /// <returns>The pull request details.</returns>
  /// <exception cref="InvalidOperationException">Thrown when GitInfo is missing or not a pull request type.</exception>
  public static async Task<GitPullRequest> GetPullRequestFromAnalysisAsync(
      this AzureDevopsClientService client,
      AnalysisResult analysisResult,
      string projectName)
  {
    if (analysisResult.GitInfo is null)
    {
      throw new InvalidOperationException("GitInfo is missing from the analysis result.");
    }

    if (analysisResult.GitInfo.Type != GitInfoType.PullRequest)
    {
      throw new InvalidOperationException($"Analysis result is not for a pull request. Type: {analysisResult.GitInfo.Type}");
    }

    if (!int.TryParse(analysisResult.GitInfo.Identifier, out var pullRequestId))
    {
      throw new InvalidOperationException($"Invalid pull request identifier: {analysisResult.GitInfo.Identifier}");
    }

    return await client.GetPullRequestAsync(
        projectName,
        analysisResult.GitInfo.RepositoryName,
        pullRequestId)
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Formats the pull request diff into a text representation suitable for AI analysis.
  /// </summary>
  /// <param name="diff">The pull request diff.</param>
  /// <returns>A formatted string containing the diff information.</returns>
  public static string FormatDiffForAI(this GitCommitDiffs diff)
  {
    var result = new System.Text.StringBuilder();

    result.AppendLine($"Pull Request Changes Summary:");
    result.AppendLine($"Total changes: {diff.ChangeCounts?.Count ?? 0}");
    result.AppendLine();

    if (diff.Changes != null)
    {
      foreach (var change in diff.Changes)
      {
        var changeType = change.ChangeType.ToString();
        var path = change.Item?.Path ?? "unknown";

        result.AppendLine($"[{changeType}] {path}");
      }
    }

    return result.ToString();
  }
}
