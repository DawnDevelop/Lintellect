using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Application.Interfaces;

/// <summary>
/// Resolves the appropriate Git client based on provider.
/// </summary>
public interface IGitClientResolver
{
    IGitClient GetClient(EGitProvider provider);
    IGitClient GetClient(AnalysisRequest analysisResult);
}