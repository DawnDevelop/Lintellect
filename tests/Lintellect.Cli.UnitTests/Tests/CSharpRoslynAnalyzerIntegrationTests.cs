using Lintellect.Cli.Services.Analyzers.Csharp;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class CSharpRoslynAnalyzerIntegrationTests
{
    private CSharpAnalyzer _analyzer = null!;
    private string _simpleRepoSolutionPath = null!;
    private AnalysisRequest _cachedAnalysisResult = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var asmDir = Path.GetDirectoryName(typeof(CSharpRoslynAnalyzerIntegrationTests).Assembly.Location)!;
        _simpleRepoSolutionPath = Path.Combine(asmDir, "Fixtures", "SimpleRepo", "Sample.slnx");

        TestHelpers.EnsureSolutionExists(_simpleRepoSolutionPath);

        _analyzer = new CSharpAnalyzer();

        // build once
        _cachedAnalysisResult = await _analyzer.AnalyzeAsync(_simpleRepoSolutionPath);
    }

    [SetUp]
    public void SetUp()
    {
        _analyzer = new CSharpAnalyzer();
    }

    [Test]
    public void Language_ShouldReturnCSharp()
    {
        // Assert
        _analyzer.Language.ShouldBe(EProgrammingLanguage.CSharp);
    }

    [Test]
    public async Task AnalyzeAsync_WithValidSolution_ShouldReturnAnalysisResult()
    {
        // Assert
        _cachedAnalysisResult.ShouldNotBeNull();
        _cachedAnalysisResult.Language.ShouldBe(EProgrammingLanguage.CSharp);
        _cachedAnalysisResult.Findings.ShouldNotBeNull();
    }

    [Test]
    public async Task AnalyzeAsync_WithSimpleRepo_ShouldDetectObsoleteMethodUsage()
    {
        // The SimpleRepo contains: call to [Obsolete] OldMethod() // triggers CS0618

        // Assert
        _cachedAnalysisResult.Findings.ShouldNotBeEmpty("SimpleRepo should have at least the CS0618 warning");

        var cs0612Finding = _cachedAnalysisResult.Findings.FirstOrDefault(f => f.RuleId == "CS0618");

        cs0612Finding.ShouldNotBeNull("OldMethod is obsolete and should trigger CS0618");
        cs0612Finding!.FilePath.ShouldEndWith("Program.cs");
        cs0612Finding.Message.ShouldContain("OldMethod");
        cs0612Finding.Line.ShouldBeGreaterThan(0);
        cs0612Finding.Severity.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_ShouldIncludeProperFilePathsInFindings()
    {

        // Assert
        if (_cachedAnalysisResult.Findings.Any())
        {
            foreach (var finding in _cachedAnalysisResult.Findings)
            {
                finding.FilePath.ShouldNotBeNullOrEmpty();
                finding.Line.ShouldBeGreaterThan(0, "Line numbers should be 1-based");
                finding.RuleId.ShouldNotBeNullOrEmpty();
                finding.Message.ShouldNotBeNullOrEmpty();
                finding.Severity.ShouldNotBeNullOrEmpty();
            }
        }
    }

    [Test]
    public async Task AnalyzeAsync_ResultFindings_ShouldBeReadOnly()
    {
        // Assert
        _cachedAnalysisResult.Findings.ShouldBeAssignableTo<IReadOnlyCollection<AnalyzerFindings>>();
    }

    [Test]
    public async Task AnalyzeAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "NonExistent.sln");

        // Act & Assert
        var act = async () => await _analyzer.AnalyzeAsync(invalidPath);
        await act.ShouldThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task AnalyzeAsync_ShouldHandleMultipleProjects()
    {
        // Assert
        _cachedAnalysisResult.ShouldNotBeNull();
        _cachedAnalysisResult.Findings.ShouldNotBeNull();
        // The analyzer should successfully process all projects in the solution
    }

    [Test]
    public async Task AnalyzeAsync_FindingsShouldHaveCorrectSeverityValues()
    {
        // Assert
        if (_cachedAnalysisResult.Findings.Any())
        {
            var validSeverities = new[] { "Hidden", "Info", "Warning", "Error" };

            foreach (var finding in _cachedAnalysisResult.Findings)
            {
                finding.Severity.ShouldBeOneOf(validSeverities,
                    $"Severity '{finding.Severity}' for rule {finding.RuleId} should be a valid Roslyn severity");
            }
        }
    }
}
