using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Models;

public record DiagnosticResult(
    string FilePath,
    int Line,
    string RuleId,
    string Message,
    string Severity,
    string AnalyzerName,
    EProgrammingLanguage Language
);
