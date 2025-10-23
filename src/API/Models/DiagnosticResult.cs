using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Models;

public record DiagnosticResult(
    string FilePath,
    int Line,
    string RuleId,
    string Message,
    string Severity,
    string AnalyzerName,
    EProgrammingLanguage Language
);
