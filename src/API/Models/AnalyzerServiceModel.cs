using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Models;

public record AnalyzerServiceModel(
    AnalysisResult AnalysisResult,
    string CopilotInstructionsPrompt
);