using Lintellect.Cli.Services;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class LanguageAnalysisOrchestratorTests
{
    [Test]
    public async Task RunAsync_WithCSharpLanguage_ShouldThrowFileNotFoundException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.CSharp);
        var solutionPath = "test.sln";

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(solutionPath);
        _ = await act.ShouldThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task RunAsync_WithUnsupportedLanguage_ShouldThrowException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.Python);
        var solutionPath = "test.sln";

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(solutionPath);
        _ = await act.ShouldThrowAsync<NotSupportedException>();
    }

    [Test]
    public async Task RunAsync_WithUnknownLanguage_ShouldThrowException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.Unknown);
        var solutionPath = "test.sln";

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(solutionPath);
        _ = await act.ShouldThrowAsync<NotSupportedException>();
    }

    [Test]
    public async Task RunAsync_WithInvalidSolutionPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.CSharp);
        var invalidPath = Path.Combine(Path.GetTempPath(), "NonExistent.sln");

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(invalidPath);
        _ = await act.ShouldThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task RunAsync_WithEmptySolutionPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.CSharp);
        var emptyPath = "";

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(emptyPath);
        _ = await act.ShouldThrowAsync<FileNotFoundException>();
    }

    [Test]
    public async Task RunAsync_WithNullSolutionPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        LanguageAnalysisOrchestrator orchestrator = new(EProgrammingLanguage.CSharp);
        string? nullPath = null;

        // Act & Assert
        Func<Task<AnalysisRequest>> act = async () => await orchestrator.RunAsync(nullPath!);
        _ = await act.ShouldThrowAsync<FileNotFoundException>();
    }

    [Test]
    public void Constructor_WithValidLanguage_ShouldNotThrow()
    {
        // Act & Assert
        _ = Should.NotThrow(() => new LanguageAnalysisOrchestrator(EProgrammingLanguage.CSharp));
    }

    [Test]
    public void Constructor_WithUnsupportedLanguage_ShouldNotThrow()
    {
        // Act & Assert
        _ = Should.NotThrow(() => new LanguageAnalysisOrchestrator(EProgrammingLanguage.Python));
    }
}
