using System.CommandLine;
using Lintellect.Cli.Commands;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class StaticAnalysisCommandTests
{
    private StaticAnalysisCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _command = [];
    }

    [Test]
    public void Constructor_ShouldSetCorrectNameAndDescription()
    {
        // Assert
        _command.Name.ShouldBe("analyze");
        _command.Description.ShouldBe("Run static analysis on code");
    }

    [Test]
    public void Constructor_ShouldHaveAllRequiredOptions()
    {
        // Assert
        _command.Options.ShouldNotBeEmpty();

        var optionNames = _command.Options.Select(o => o.Name).ToList();
        optionNames.ShouldContain("--solution");
        optionNames.ShouldContain("--api-url");
        optionNames.ShouldContain("--api-key");
        optionNames.ShouldContain("--language");
        optionNames.ShouldContain("--exclude");
        optionNames.ShouldContain("--enable-summary-comment");
        optionNames.ShouldContain("--enable-inline-suggestions");
        optionNames.ShouldContain("--enable-description-summary");
        optionNames.ShouldContain("--enable-azure-devops-code-owners");
        optionNames.ShouldContain("--enable-semgrep");
    }

    [Test]
    public void SolutionOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var solutionOption = _command.Options.OfType<Option<string>>().First(o => o.Name == "--solution");

        // Assert
        solutionOption.Description.ShouldBe("Path to .sln or .slnx (default: current directory)");
        _ = solutionOption.DefaultValueFactory.ShouldNotBeNull();
        solutionOption.Validators.ShouldNotBeEmpty();
    }

    [Test]
    public void ApiUrlOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var apiUrlOption = _command.Options.OfType<Option<string>>().First(o => o.Name == "--api-url");

        // Assert
        apiUrlOption.Validators.ShouldNotBeEmpty();
    }

    [Test]
    public void LanguageOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var languageOption = _command.Options.OfType<Option<EProgrammingLanguage>>().First(o => o.Name == "--language");

        // Assert
        languageOption.Description.ShouldBe("Programming language (default: csharp)");
        _ = languageOption.DefaultValueFactory.ShouldNotBeNull();
    }

    [Test]
    public void LanguageOption_ShouldDefaultToCSharp()
    {
        // Arrange
        var languageOption = _command.Options.OfType<Option<EProgrammingLanguage>>().First(o => o.Name == "--language");

        // Act
        var defaultValue = languageOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(EProgrammingLanguage.CSharp);
    }

    [Test]
    public void ExclusionsOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var exclusionsOption = _command.Options.OfType<Option<string[]>>().First(o => o.Name == "--exclude");

        // Assert
        exclusionsOption.Description.ShouldBe("File/folder patterns to exclude from analysis (e.g., '**/bin/**', '**/obj/**') (default: none)");
        exclusionsOption.AllowMultipleArgumentsPerToken.ShouldBeTrue();
    }

    [Test]
    public void EnableSummaryCommentOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var enableSummaryCommentOption = _command.Options.OfType<Option<bool>>().First(o => o.Name == "--enable-summary-comment");

        // Act
        var defaultValue = enableSummaryCommentOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(true);
    }

    [Test]
    public void EnableInlineSuggestionsOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var enableInlineSuggestionsOption = _command.Options.OfType<Option<bool>>().First(o => o.Name == "--enable-inline-suggestions");

        // Act
        var defaultValue = enableInlineSuggestionsOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(true);
    }

    [Test]
    public void EnableDescriptionSummaryOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var enableDescriptionSummaryOption = _command.Options.OfType<Option<bool>>().First(o => o.Name == "--enable-description-summary");

        // Act
        var defaultValue = enableDescriptionSummaryOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(true);
    }

    [Test]
    public void EnableAzureDevopsCodeOwnersOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var enableCodeOwnersOption = _command.Options.OfType<Option<bool>>().First(o => o.Name == "--enable-azure-devops-code-owners");

        // Act
        var defaultValue = enableCodeOwnersOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(false);
    }

    [Test]
    public void EnableSemgrepOption_ShouldHaveCorrectProperties()
    {
        // Arrange
        var enableSemgrepOption = _command.Options.OfType<Option<bool>>().First(o => o.Name == "--enable-semgrep");

        // Act
        var defaultValue = enableSemgrepOption.DefaultValueFactory?.Invoke(null!);

        // Assert
        defaultValue.ShouldBe(true);
    }

    [Test]
    public void Command_ShouldHaveAction()
    {
        // Assert
        _ = _command.Action.ShouldNotBeNull();
    }
}
