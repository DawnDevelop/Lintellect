using devops_pr_analyzer.cli.Services.Analyzers.Csharp;
using devops_pr_analyzer.shared.Models;
using FluentAssertions;

namespace devops_pr_analyzer.cli.integrationtests.Tests;

[TestFixture]
public class CSharpRoslynAnalyzerIntegrationTests
{
    private CSharpAnalyzer _analyzer = null!;
    private string _simpleRepoSolutionPath = null!;
    private AnalysisResult _cachedAnalysisResult = null!;

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
        _analyzer.Language.Should().Be(EProgrammingLanguage.CSharp);
    }

    [Test]
    public async Task AnalyzeAsync_WithValidSolution_ShouldReturnAnalysisResult()
    {
        // Assert
        _cachedAnalysisResult.Should().NotBeNull();
        _cachedAnalysisResult.Language.Should().Be("CSharp");
        _cachedAnalysisResult.Findings.Should().NotBeNull();
    }

    [Test]
    public async Task AnalyzeAsync_WithSimpleRepo_ShouldDetectObsoleteMethodUsage()
    {
        // The SimpleRepo contains: call to [Obsolete] OldMethod() // triggers CS0618

        // Assert
        _cachedAnalysisResult.Findings.Should().NotBeEmpty("SimpleRepo should have at least the CS0618 warning");

        var cs0612Finding = _cachedAnalysisResult.Findings.FirstOrDefault(f => f.RuleId == "CS0618");

        cs0612Finding.Should().NotBeNull("OldMethod is obsolete and should trigger CS0618");
        cs0612Finding!.FilePath.Should().EndWith("Program.cs");
        cs0612Finding.Message.Should().Contain("OldMethod");
        cs0612Finding.Line.Should().BeGreaterThan(0);
        cs0612Finding.Severity.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_ShouldIncludeProperFilePathsInFindings()
    {

        // Assert
        if (_cachedAnalysisResult.Findings.Any())
        {
            foreach (var finding in _cachedAnalysisResult.Findings)
            {
                finding.FilePath.Should().NotBeNullOrEmpty();
                finding.Line.Should().BeGreaterThan(0, "Line numbers should be 1-based");
                finding.RuleId.Should().NotBeNullOrEmpty();
                finding.Message.Should().NotBeNullOrEmpty();
                finding.Severity.Should().NotBeNullOrEmpty();
            }
        }
    }

    [Test]
    public async Task AnalyzeAsync_ResultFindings_ShouldBeReadOnly()
    {
        // Assert
        _cachedAnalysisResult.Findings.Should().BeAssignableTo<IReadOnlyCollection<AnalyzerFindings>>();
    }

    [Test]
    public async Task AnalyzeAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "NonExistent.sln");

        // Act & Assert
        var act = async () => await _analyzer.AnalyzeAsync(invalidPath);
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task AnalyzeAsync_ShouldHandleMultipleProjects()
    {
        // Assert
        _cachedAnalysisResult.Should().NotBeNull();
        _cachedAnalysisResult.Findings.Should().NotBeNull();
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
                finding.Severity.Should().BeOneOf(validSeverities, 
                    $"Severity '{finding.Severity}' for rule {finding.RuleId} should be a valid Roslyn severity");
            }
        }
    }
}
