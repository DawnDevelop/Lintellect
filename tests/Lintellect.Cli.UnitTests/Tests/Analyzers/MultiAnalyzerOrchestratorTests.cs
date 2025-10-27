using System.Reflection;
using Lintellect.Cli.Interfaces;
using Lintellect.Cli.Services;
using Lintellect.Cli.Services.Analyzers.Csharp;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests.Analyzers;

[TestFixture]
public class MultiAnalyzerOrchestratorTests
{
    [Test]
    public void Constructor_WithCSharpLanguage_ShouldCreateCSharpAnalyzer()
    {
        // Arrange & Act
        var orchestrator = new AnalysisOrchestrator(EProgrammingLanguage.CSharp);

        // Assert
        var analyzer = InvokePrivateMethod<ICodeAnalyzer?>(orchestrator, "_codeAnalyzer");
        analyzer.ShouldNotBeNull();
        analyzer!.GetType().ShouldBe(typeof(CSharpAnalyzer));
        analyzer.Language.ShouldBe(EProgrammingLanguage.CSharp);
    }

    [Test]
    public void Constructor_WithUnknownLanguage_ShouldReturnNullAnalyzer()
    {
        // Arrange & Act
        var orchestrator = new AnalysisOrchestrator(EProgrammingLanguage.Unknown);

        // Assert
        var analyzer = InvokePrivateMethod<ICodeAnalyzer?>(orchestrator, "_codeAnalyzer");
        analyzer.ShouldBeNull();
    }

    [Test]
    public void Constructor_WithSemgrepEnabled_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, enableSemgrep: true));
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.Python, enableSemgrep: true));
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.Java, enableSemgrep: false));
    }

    [Test]
    public void Constructor_WithExclusionPatterns_ShouldNotThrow()
    {
        // Arrange
        var exclusionPatterns = new List<string> { "**/bin/**", "**/obj/**" };

        // Act & Assert
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, exclusionPatterns: exclusionPatterns));
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, enableSemgrep: true, exclusionPatterns: exclusionPatterns));
    }

    [Test]
    public void Constructor_WithAllLanguages_ShouldNotThrow()
    {
        // Act & Assert
        var languages = Enum.GetValues<EProgrammingLanguage>();
        foreach (var language in languages)
        {
            Should.NotThrow(() => new AnalysisOrchestrator(language, enableSemgrep: false));
            Should.NotThrow(() => new AnalysisOrchestrator(language, enableSemgrep: true));
        }
    }

    [Test]
    public void Constructor_WithNullExclusionPatterns_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, exclusionPatterns: null));
    }

    [Test]
    public void Constructor_WithEmptyExclusionPatterns_ShouldNotThrow()
    {
        // Arrange
        var emptyPatterns = new List<string>();

        // Act & Assert
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, exclusionPatterns: emptyPatterns));
    }

    private T InvokePrivateMethod<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found");
        }

        var result = field.GetValue(instance);
        return (T)result!;
    }
}

