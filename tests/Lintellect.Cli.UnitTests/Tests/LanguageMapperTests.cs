using Lintellect.Cli.Extensions;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class LanguageMapperTests
{
    [TestCase("test.cs", EProgrammingLanguage.CSharp)]
    [TestCase("Program.cs", EProgrammingLanguage.CSharp)]
    [TestCase("MyClass.cs", EProgrammingLanguage.CSharp)]
    [TestCase("test.py", EProgrammingLanguage.Python)]
    [TestCase("script.py", EProgrammingLanguage.Python)]
    [TestCase("main.py", EProgrammingLanguage.Python)]
    [TestCase("App.java", EProgrammingLanguage.Java)]
    [TestCase("Test.java", EProgrammingLanguage.Java)]
    [TestCase("Main.java", EProgrammingLanguage.Java)]
    [TestCase("app.js", EProgrammingLanguage.JavaScript)]
    [TestCase("script.js", EProgrammingLanguage.JavaScript)]
    [TestCase("index.js", EProgrammingLanguage.JavaScript)]
    [TestCase("app.ts", EProgrammingLanguage.TypeScript)]
    [TestCase("component.ts", EProgrammingLanguage.TypeScript)]
    [TestCase("main.ts", EProgrammingLanguage.TypeScript)]
    [TestCase("main.go", EProgrammingLanguage.Go)]
    [TestCase("server.go", EProgrammingLanguage.Go)]
    [TestCase("handler.go", EProgrammingLanguage.Go)]
    public void FromFileName_WithValidExtensions_ShouldReturnCorrectLanguage(string fileName, EProgrammingLanguage expectedLanguage)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldBe(expectedLanguage);
    }

    [TestCase("TEST.CS")]
    [TestCase("PROGRAM.Cs")]
    [TestCase("MyClass.Cs")]
    [TestCase("SCRIPT.PY")]
    [TestCase("MAIN.Py")]
    [TestCase("APP.JS")]
    [TestCase("COMPONENT.TS")]
    [TestCase("SERVER.GO")]
    public void FromFileName_WithUpperCaseExtensions_ShouldReturnCorrectLanguage(string fileName)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldNotBe(EProgrammingLanguage.Unknown);
    }

    [TestCase("test.txt")]
    [TestCase("README.md")]
    [TestCase("config.xml")]
    [TestCase("data.json")]
    [TestCase("style.css")]
    [TestCase("index.html")]
    [TestCase("Dockerfile")]
    [TestCase("Makefile")]
    [TestCase("file")]
    public void FromFileName_WithUnknownExtensions_ShouldReturnUnknown(string fileName)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldBe(EProgrammingLanguage.Unknown);
    }

    [TestCase("test.cs.bak")]
    [TestCase("backup.py.old")]
    [TestCase("temp.js.tmp")]
    public void FromFileName_WithMultipleExtensions_ShouldUseLastExtension(string fileName)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldBe(EProgrammingLanguage.Unknown);
    }

    [TestCase("test")]
    [TestCase("file")]
    [TestCase("noextension")]
    public void FromFileName_WithNoExtension_ShouldReturnUnknown(string fileName)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldBe(EProgrammingLanguage.Unknown);
    }

    [TestCase(".cs")]
    [TestCase(".py")]
    [TestCase(".js")]
    [TestCase(".ts")]
    [TestCase(".go")]
    [TestCase(".java")]
    public void FromFileName_WithOnlyExtension_ShouldReturnCorrectLanguage(string fileName)
    {
        // Act
        var result = LanguageMapper.FromFileName(fileName);

        // Assert
        result.ShouldNotBe(EProgrammingLanguage.Unknown);
    }

    [Test]
    public void FromFileName_WithNullFileName_ShouldReturnUnknown()
    {
        // Act
        var result = LanguageMapper.FromFileName(null!);

        // Assert
        result.ShouldBe(EProgrammingLanguage.Unknown);
    }

    [Test]
    public void FromFileName_WithWhitespaceFileName_ShouldReturnUnknown()
    {
        // Act
        var result = LanguageMapper.FromFileName("   ");

        // Assert
        result.ShouldBe(EProgrammingLanguage.Unknown);
    }
}
