using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class GitHubInfoExtractorTests
{
    private GitHubInfoExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new GitHubInfoExtractor();
    }

    [Test]
    public void ExtractInfo_WithValidGitHubEnvironment_ShouldReturnGitInfo()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/123/merge",
            ["GITHUB_SHA"] = "abc123def456",
            ["GITHUB_REPOSITORY"] = "owner/repo"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(123);
        result.CommitId.ShouldBe("abc123def456");
        result.RepositoryName.ShouldBe("owner/repo");
        result.Type.ShouldBe(EGitInfoType.Unknown); // Default value
    }

    [Test]
    public void ExtractInfo_WithHeadRef_ShouldExtractPullRequestNumber()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/456/head",
            ["GITHUB_SHA"] = "def456ghi789",
            ["GITHUB_REPOSITORY"] = "test/example"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(456);
    }

    [Test]
    public void ExtractInfo_WithMissingGitHubRef_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_SHA"] = "abc123def456",
            ["GITHUB_REPOSITORY"] = "owner/repo"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithMissingCommitId_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/123/merge",
            ["GITHUB_REPOSITORY"] = "owner/repo"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithMissingRepository_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/123/merge",
            ["GITHUB_SHA"] = "abc123def456"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithInvalidGitHubRef_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/heads/main",
            ["GITHUB_SHA"] = "abc123def456",
            ["GITHUB_REPOSITORY"] = "owner/repo"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithMalformedGitHubRef_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/invalid/merge",
            ["GITHUB_SHA"] = "abc123def456",
            ["GITHUB_REPOSITORY"] = "owner/repo"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithEmptyValues_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "",
            ["GITHUB_SHA"] = "",
            ["GITHUB_REPOSITORY"] = ""
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithWhitespaceValues_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "   ",
            ["GITHUB_SHA"] = "   ",
            ["GITHUB_REPOSITORY"] = "   "
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }
}
