using System.Reflection;
using Lintellect.Cli.Services;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class MultiAnalyzerOrchestratorTests
{
    [TestCase(EProgrammingLanguage.CSharp, typeof(CodeQLCSharpAnalyzer))]
    [TestCase(EProgrammingLanguage.Python, typeof(CodeQLPythonAnalyzer))]
    [TestCase(EProgrammingLanguage.Java, typeof(CodeQLJavaAnalyzer))]
    [TestCase(EProgrammingLanguage.JavaScript, typeof(CodeQLJavaScriptAnalyzer))]
    [TestCase(EProgrammingLanguage.TypeScript, typeof(CodeQLTypeScriptAnalyzer))]
    [TestCase(EProgrammingLanguage.Go, typeof(CodeQLGoAnalyzer))]
    [TestCase(EProgrammingLanguage.Ruby, typeof(CodeQLRubyAnalyzer))]
    [TestCase(EProgrammingLanguage.PHP, typeof(CodeQLPhpAnalyzer))]
    [TestCase(EProgrammingLanguage.Swift, typeof(CodeQLSwiftAnalyzer))]
    [TestCase(EProgrammingLanguage.Kotlin, typeof(CodeQLKotlinAnalyzer))]
    public void CreateCodeQLAnalyzer_WithSupportedLanguage_ShouldReturnCorrectAnalyzer(EProgrammingLanguage language, Type expectedAnalyzerType)
    {
        // Arrange
        var orchestrator = new AnalysisOrchestrator(language, enableCodeQL: true);

        // Act
        var analyzer = InvokePrivateMethod<CodeQLAnalyzerBase?>(orchestrator, "CreateCodeQLAnalyzer");

        // Assert
        analyzer.ShouldNotBeNull();
        analyzer!.GetType().ShouldBe(expectedAnalyzerType);
        analyzer.Language.ShouldBe(language);
    }

    [Test]
    public void CreateCodeQLAnalyzer_WithUnknownLanguage_ShouldReturnNull()
    {
        // Arrange
        var orchestrator = new AnalysisOrchestrator(EProgrammingLanguage.Unknown, enableCodeQL: true);

        // Act
        var analyzer = InvokePrivateMethod<CodeQLAnalyzerBase?>(orchestrator, "CreateCodeQLAnalyzer");

        // Assert
        analyzer.ShouldBeNull();
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.CSharp, enableCodeQL: true, githubToken: "test-token"));
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.Python, enableCodeQL: false));
        Should.NotThrow(() => new AnalysisOrchestrator(EProgrammingLanguage.Java, enableCodeQL: true));
    }

    [Test]
    public void Constructor_WithAllLanguages_ShouldNotThrow()
    {
        // Act & Assert
        var languages = Enum.GetValues<EProgrammingLanguage>();
        foreach (var language in languages)
        {
            Should.NotThrow(() => new AnalysisOrchestrator(language, enableCodeQL: true));
        }
    }

    private T InvokePrivateMethod<T>(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found");
        }

        var result = method.Invoke(instance, parameters);
        return (T)result!;
    }
}
