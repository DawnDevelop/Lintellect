using Lintellect.Cli.Services.Git;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class GitInfoExtractorFactoryTests
{
    [Test]
    public void Create_WithAzureDevOpsEnvironment_ShouldReturnAzureDevOpsInfoExtractor()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["SYSTEM_TEAMFOUNDATIONCOLLECTIONURI"] = "https://dev.azure.com/myorg"
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<AzureDevOpsInfoExtractor>();
    }

    [Test]
    public void Create_WithGitHubActionsEnvironment_ShouldReturnGitHubInfoExtractor()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "true"
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<GitHubInfoExtractor>();
    }

    [Test]
    public void Create_WithBothEnvironments_ShouldPrioritizeAzureDevOps()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["SYSTEM_TEAMFOUNDATIONCOLLECTIONURI"] = "https://dev.azure.com/myorg",
            ["GITHUB_ACTIONS"] = "true"
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<AzureDevOpsInfoExtractor>();
    }

    [Test]
    public void Create_WithNoEnvironment_ShouldReturnNoOpChangeDetector()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables([]);

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<NoOpChangeDetector>();
    }

    [Test]
    public void Create_WithEmptyEnvironment_ShouldReturnNoOpChangeDetector()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["SYSTEM_TEAMFOUNDATIONCOLLECTIONURI"] = null,
            ["GITHUB_ACTIONS"] = null
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<NoOpChangeDetector>();
    }

    [Test]
    public void Create_WithWhitespaceEnvironment_ShouldReturnNoOpChangeDetector()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["SYSTEM_TEAMFOUNDATIONCOLLECTIONURI"] = null,
            ["GITHUB_ACTIONS"] = null
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<NoOpChangeDetector>();
    }

    [Test]
    public void Create_WithGitHubActionsFalse_ShouldReturnNoOpChangeDetector()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "false"
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<NoOpChangeDetector>();
    }

    [Test]
    public void Create_WithGitHubActionsTrue_ShouldReturnGitHubInfoExtractor()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "true"
        });

        // Act
        var result = GitInfoExtractorFactory.Create();

        // Assert
        _ = result.ShouldBeOfType<GitHubInfoExtractor>();
    }
}
