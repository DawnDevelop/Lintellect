using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Models;

public record AnalyzerServiceModel(
    AnalysisRequest AnalysisResult,
    string CopilotInstructionsPrompt
);
