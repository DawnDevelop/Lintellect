using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Application.Models;

public record AnalyzerServiceModel(
    AnalysisRequest AnalysisResult,
    string CopilotInstructionsPrompt
);