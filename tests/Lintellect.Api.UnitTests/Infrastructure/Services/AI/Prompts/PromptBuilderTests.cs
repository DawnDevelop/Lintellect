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
    public void BuildInlineSuggestionsPrompt_FiltersFindingsOutsidePrioritizedFiles()
    {
        var diffs = Enumerable.Range(1, 11)
            .ToDictionary(i => $"File{i:00}.cs", _ => "+ 1:Console.WriteLine();");

        var findings = Enumerable.Range(1, 11)
            .Select(i => new AnalyzerFindings
            {
                RuleId = $"TEST{i:000}",
                FilePath = $"File{i:00}.cs",
                Line = i,
                Severity = i == 11 ? "Info" : "Error",
                Message = $"Message {i}"
            })
            .ToList();

        var prompt = _sut.BuildInlineSuggestionsPrompt(CreateRequest(findings), diffs);

        prompt.ShouldContain("### File: `File01.cs`");
        prompt.ShouldNotContain("### File: `File11.cs`");
    }

    [Test]
    public void BuildInlineSuggestionsPrompt_TruncatesLargeDiffs()
    {
        var diff = string.Join('\n', Enumerable.Range(1, 120).Select(i => $"+ {i:D3}:Console.WriteLine({i});"));
        var diffs = new Dictionary<string, string>
        {
            ["src/LargeFile.cs"] = diff
        };

        var findings = new List<AnalyzerFindings>
        {
            new()
            {
                RuleId = "TEST999",
                Message = "Large diff",
                FilePath = "src/LargeFile.cs",
                Line = 1,
                Severity = "Warning"
            }
        };

        var prompt = _sut.BuildInlineSuggestionsPrompt(CreateRequest(findings), diffs);

        prompt.ShouldContain("... (truncated)");
        prompt.ShouldContain("+ 100:Console.WriteLine(100);");
        prompt.ShouldNotContain("+ 120:Console.WriteLine(120);");
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

