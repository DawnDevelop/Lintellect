using Lintellect.Cli.Services.Analyzers;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class SemgrepAnalyzerTests
{
    private SemgrepAnalyzer _analyzer = null!;
    private string _testSolutionPath = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new SemgrepAnalyzer(EProgrammingLanguage.CSharp);
        _testSolutionPath = Path.Combine(Path.GetTempPath(), "TestSolution.sln");

        // Create a temporary solution file for testing
        File.WriteAllText(_testSolutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject", "TestProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Global
                GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                EndGlobalSection
                GlobalSection(ProjectConfigurationPlatforms) = postSolution
                    {12345678-1234-1234-1234-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                EndGlobalSection
            EndGlobal
            """);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testSolutionPath))
        {
            File.Delete(_testSolutionPath);
        }
    }

    [Test]
    public void Language_ShouldReturnCorrectLanguage()
    {
        // Arrange
        var csharpAnalyzer = new SemgrepAnalyzer(EProgrammingLanguage.CSharp);
        var pythonAnalyzer = new SemgrepAnalyzer(EProgrammingLanguage.Python);
        var javaAnalyzer = new SemgrepAnalyzer(EProgrammingLanguage.Java);

        // Assert
        csharpAnalyzer.Language.ShouldBe(EProgrammingLanguage.CSharp);
        pythonAnalyzer.Language.ShouldBe(EProgrammingLanguage.Python);
        javaAnalyzer.Language.ShouldBe(EProgrammingLanguage.Java);
    }

    [Test]
    public async Task AnalyzeAsync_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent.sln");

        // Act
        var result = await _analyzer.AnalyzeAsync(nonExistentPath);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_WithNullPath_ShouldReturnEmptyList()
    {
        // Act
        var result = await _analyzer.AnalyzeAsync(null!);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_WithEmptyPath_ShouldReturnEmptyList()
    {
        // Act
        var result = await _analyzer.AnalyzeAsync("");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_ResultShouldBeReadOnlyCollection()
    {
        // Act
        var result = await _analyzer.AnalyzeAsync(_testSolutionPath);

        // Assert
        result.ShouldBeAssignableTo<IReadOnlyCollection<AnalyzerFindings>>();
    }

    [Test]
    public async Task AnalyzeAsync_ShouldHandleDockerNotAvailable()
    {

        // Act
        var result = await _analyzer.AnalyzeAsync(_testSolutionPath);

        // Assert
        result.ShouldNotBeNull();
        // Note: We can't easily test Docker availability in unit tests,
        // but we can verify the method doesn't throw exceptions
    }

    [Test]
    public void Constructor_WithAllSupportedLanguages_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.CSharp));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Python));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Java));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.JavaScript));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.TypeScript));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Go));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Ruby));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.PHP));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Swift));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Kotlin));
        Should.NotThrow(() => new SemgrepAnalyzer(EProgrammingLanguage.Unknown));
    }

    [Test]
    public async Task AnalyzeAsync_ShouldReturnConsistentResults()
    {
        // Act - Run analysis multiple times
        var result1 = await _analyzer.AnalyzeAsync(_testSolutionPath);
        var result2 = await _analyzer.AnalyzeAsync(_testSolutionPath);

        // Assert - Results should be consistent (both empty or both have same findings)
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();

        // If Docker is available and Semgrep runs successfully, results should be identical
        // If Docker is not available, both should return empty lists
        if (result1.Count != 0 && result2.Count != 0)
        {
            result1.Count.ShouldBe(result2.Count);
            // Additional consistency checks could be added here
        }
    }

    [Test]
    public async Task AnalyzeAsync_WithValidSolution_ShouldNotThrowException()
    {
        // Act & Assert
        var act = async () => await _analyzer.AnalyzeAsync(_testSolutionPath);
        await act.ShouldNotThrowAsync();
    }

    [Test]
    public async Task AnalyzeAsync_WithInvalidCharactersInPath_ShouldHandleGracefully()
    {
        // Arrange - Use a path that's valid for the file system but might cause issues
        var invalidPath = Path.Combine(Path.GetTempPath(), "Test Solution with Spaces.sln");
        File.WriteAllText(invalidPath, "Invalid solution content");

        try
        {
            // Act
            var result = await _analyzer.AnalyzeAsync(invalidPath);

            // Assert
            result.ShouldNotBeNull();
            // Should not throw exception even with spaces in path
        }
        finally
        {
            if (File.Exists(invalidPath))
            {
                File.Delete(invalidPath);
            }
        }
    }

    [Test]
    public async Task AnalyzeAsync_WithVeryLongPath_ShouldHandleGracefully()
    {
        // Arrange
        var longPath = Path.Combine(Path.GetTempPath(), new string('A', 200) + ".sln");
        File.WriteAllText(longPath, "Test solution content");

        try
        {
            // Act
            var result = await _analyzer.AnalyzeAsync(longPath);

            // Assert
            result.ShouldNotBeNull();
            // Should handle long paths gracefully
        }
        finally
        {
            if (File.Exists(longPath))
            {
                File.Delete(longPath);
            }
        }
    }
}
