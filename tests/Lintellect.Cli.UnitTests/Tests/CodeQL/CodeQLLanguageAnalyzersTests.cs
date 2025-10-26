using System.Reflection;
using Lintellect.Cli.Interfaces;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests.CodeQL;

[TestFixture]
public class CodeQLLanguageAnalyzersTests : CodeQLTestBase
{
    [TestCase(typeof(CodeQLCSharpAnalyzer), EProgrammingLanguage.CSharp, "csharp", "artifacts,obj,bin,debug,release,publish,out,packages,node_modules,wwwroot/lib,TestResults,coverage,.vs,.vscode,.idea")]
    [TestCase(typeof(CodeQLPythonAnalyzer), EProgrammingLanguage.Python, "python", "__pycache__,.venv,venv,env,.pytest_cache,.tox,dist,build,*.egg-info,node_modules,coverage")]
    [TestCase(typeof(CodeQLJavaAnalyzer), EProgrammingLanguage.Java, "java", "target,build,.gradle,out,*.class,node_modules,coverage")]
    [TestCase(typeof(CodeQLJavaScriptAnalyzer), EProgrammingLanguage.JavaScript, "javascript", "node_modules,dist,build,coverage,.next,.nuxt,out")]
    [TestCase(typeof(CodeQLTypeScriptAnalyzer), EProgrammingLanguage.TypeScript, "javascript", "node_modules,dist,build,coverage,.next,.nuxt,out")]
    [TestCase(typeof(CodeQLGoAnalyzer), EProgrammingLanguage.Go, "go", "vendor,bin,pkg,node_modules,coverage")]
    [TestCase(typeof(CodeQLRubyAnalyzer), EProgrammingLanguage.Ruby, "ruby", "vendor,.bundle,coverage,tmp,node_modules")]
    [TestCase(typeof(CodeQLPhpAnalyzer), EProgrammingLanguage.PHP, "php", "vendor,cache,storage,node_modules,coverage")]
    [TestCase(typeof(CodeQLSwiftAnalyzer), EProgrammingLanguage.Swift, "swift", ".build,Packages,*.xcodeproj,DerivedData,node_modules,coverage")]
    [TestCase(typeof(CodeQLKotlinAnalyzer), EProgrammingLanguage.Kotlin, "java", "target,build,.gradle,out,*.class,node_modules,coverage")]
    public void Analyzer_ShouldHaveCorrectProperties(Type analyzerType, EProgrammingLanguage expectedLanguage, string expectedIdentifier, string expectedSkipDirs)
    {
        // Arrange
        var analyzer = (CodeQLAnalyzerBase)Activator.CreateInstance(analyzerType)!;

        // Act & Assert
        AssertAnalyzerProperties(analyzer, expectedLanguage, expectedIdentifier, expectedSkipDirs);
    }

    [Test]
    public void AllAnalyzers_ShouldImplementICodeAnalyzer()
    {
        // Arrange
        var analyzerTypes = new[]
        {
            typeof(CodeQLCSharpAnalyzer),
            typeof(CodeQLPythonAnalyzer),
            typeof(CodeQLJavaAnalyzer),
            typeof(CodeQLJavaScriptAnalyzer),
            typeof(CodeQLTypeScriptAnalyzer),
            typeof(CodeQLGoAnalyzer),
            typeof(CodeQLRubyAnalyzer),
            typeof(CodeQLPhpAnalyzer),
            typeof(CodeQLSwiftAnalyzer),
            typeof(CodeQLKotlinAnalyzer)
        };

        // Act & Assert
        foreach (var analyzerType in analyzerTypes)
        {
            var analyzer = Activator.CreateInstance(analyzerType)!;
            analyzer.ShouldBeAssignableTo<ICodeAnalyzer>();
        }
    }

    [Test]
    public void AllAnalyzers_ShouldInheritFromCodeQLAnalyzerBase()
    {
        // Arrange
        var analyzerTypes = new[]
        {
            typeof(CodeQLCSharpAnalyzer),
            typeof(CodeQLPythonAnalyzer),
            typeof(CodeQLJavaAnalyzer),
            typeof(CodeQLJavaScriptAnalyzer),
            typeof(CodeQLTypeScriptAnalyzer),
            typeof(CodeQLGoAnalyzer),
            typeof(CodeQLRubyAnalyzer),
            typeof(CodeQLPhpAnalyzer),
            typeof(CodeQLSwiftAnalyzer),
            typeof(CodeQLKotlinAnalyzer)
        };

        // Act & Assert
        foreach (var analyzerType in analyzerTypes)
        {
            analyzerType.BaseType.ShouldBe(typeof(CodeQLAnalyzerBase));
        }
    }

    [Test]
    public void GetLanguageIdentifier_ShouldReturnValidCodeQLLanguage()
    {
        // Arrange
        var validIdentifiers = new[] { "csharp", "python", "java", "javascript", "go", "ruby", "php", "swift" };
        var analyzerTypes = new[]
        {
            typeof(CodeQLCSharpAnalyzer),
            typeof(CodeQLPythonAnalyzer),
            typeof(CodeQLJavaAnalyzer),
            typeof(CodeQLJavaScriptAnalyzer),
            typeof(CodeQLTypeScriptAnalyzer),
            typeof(CodeQLGoAnalyzer),
            typeof(CodeQLRubyAnalyzer),
            typeof(CodeQLPhpAnalyzer),
            typeof(CodeQLSwiftAnalyzer),
            typeof(CodeQLKotlinAnalyzer)
        };

        // Act & Assert
        foreach (var analyzerType in analyzerTypes)
        {
            var analyzer = (CodeQLAnalyzerBase)Activator.CreateInstance(analyzerType)!;
            var identifierMethod = analyzer.GetType().GetMethod("GetLanguageIdentifier", BindingFlags.NonPublic | BindingFlags.Instance);
            var identifier = identifierMethod?.Invoke(analyzer, null)?.ToString();

            validIdentifiers.ShouldContain(identifier, $"Language identifier '{identifier}' for {analyzerType.Name} should be valid");
        }
    }

    [Test]
    public void GetSkipDirectories_ShouldNotBeEmpty()
    {
        // Arrange
        var analyzerTypes = new[]
        {
            typeof(CodeQLCSharpAnalyzer),
            typeof(CodeQLPythonAnalyzer),
            typeof(CodeQLJavaAnalyzer),
            typeof(CodeQLJavaScriptAnalyzer),
            typeof(CodeQLTypeScriptAnalyzer),
            typeof(CodeQLGoAnalyzer),
            typeof(CodeQLRubyAnalyzer),
            typeof(CodeQLPhpAnalyzer),
            typeof(CodeQLSwiftAnalyzer),
            typeof(CodeQLKotlinAnalyzer)
        };

        // Act & Assert
        foreach (var analyzerType in analyzerTypes)
        {
            var analyzer = (CodeQLAnalyzerBase)Activator.CreateInstance(analyzerType)!;
            var skipDirsMethod = analyzer.GetType().GetMethod("GetSkipDirectories", BindingFlags.NonPublic | BindingFlags.Instance);
            var skipDirs = skipDirsMethod?.Invoke(analyzer, null)?.ToString();

            skipDirs.ShouldNotBeNullOrEmpty($"Skip directories for {analyzerType.Name} should not be empty");
        }
    }
}
