using Lintellect.Api.Infrastructure.Services.AI.Prompts;

namespace Lintellect.Api.UnitTests.Infrastructure.Services.AI.Prompts;

[TestFixture]
public class PromptBuilderTests
{
    private readonly PromptBuilder _sut = new();

    [Test]
    public void BuildInlineSuggestionsPrompt_WithShortMessage_IncludesMessageWithoutThrowing()
    {
        var diffs = new Dictionary<string, string>
        {
            ["src/File.cs"] = "+ 10:Console.WriteLine();"
        };

        var findings = new List<AnalyzerFindings>
        {
            new()
            {
                RuleId = "TEST001",
                Message = "Short message",
                FilePath = "src/File.cs",
                Line = 10,
                Severity = "Error"
            }
        };

        var prompt = _sut.BuildInlineSuggestionsPrompt(CreateRequest(findings), diffs);

        prompt.ShouldContain("Short message");
        prompt.ShouldContain("### File: `src/File.cs`");
    }

    [Test]
    public void BuildAnalysisPrompt_ReportsLanguageAndFindingCounts()
    {
        var findings = new List<AnalyzerFindings>
        {
            new()
            {
                RuleId = "ERR001",
                Message = "Error",
                FilePath = "FileA.cs",
                Line = 1,
                Severity = "Error"
            },
            new()
            {
                RuleId = "WARN001",
                Message = "Warn",
                FilePath = "FileB.cs",
                Line = 2,
                Severity = "Warning"
            },
            new()
            {
                RuleId = "INFO001",
                Message = "Info",
                FilePath = "FileC.cs",
                Line = 3,
                Severity = "Info"
            }
        };

        var prompt = _sut.BuildAnalysisPrompt(CreateRequest(findings), new Dictionary<string, string>());

        prompt.ShouldContain("## Static Analysis Results");
        prompt.ShouldContain("- **Language**: CSharp");
        prompt.ShouldContain("**Total Findings**: 3");
        prompt.ShouldContain("ERR001");
        prompt.ShouldContain("WARN001");
    }

    private static AnalysisRequest CreateRequest(IEnumerable<AnalyzerFindings>? findings = null)
    {
        return new AnalysisRequest
        {
            Language = EProgrammingLanguage.CSharp,
            Findings = findings?.ToList() ?? []
        };
    }
}

