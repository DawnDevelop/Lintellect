using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class AzureDevOpsInfoExtractorTests
{
    private AzureDevOpsInfoExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new AzureDevOpsInfoExtractor();
    }

    [Test]
    public void ExtractInfo_WithPullRequestEnvironment_ShouldReturnGitInfo()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["SYSTEM_PULLREQUEST_PULLREQUESTID"] = "123",
            ["BUILD_SOURCEVERSION"] = "abc123def456",
            ["BUILD_REPOSITORY_NAME"] = "MyProject",
            ["SYSTEM_TEAMPROJECT"] = "MyTeamProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(123);
        result.CommitId.ShouldBe("abc123def456");
        result.RepositoryName.ShouldBe("MyProject");
        result.Type.ShouldBe(EGitInfoType.PullRequest);
        result.ProjectName.ShouldBe("MyTeamProject");
    }

    [Test]
    public void ExtractInfo_WithCIBuildEnvironment_ShouldReturnGitInfo()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "def456ghi789",
            ["BUILD_REPOSITORY_NAME"] = "TestRepo",
            ["BUILD_REASON"] = "IndividualCI",
            ["BUILD_BUILDID"] = "456",
            ["SYSTEM_TEAMPROJECT"] = "TestProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(456);
        result.CommitId.ShouldBe("def456ghi789");
        result.RepositoryName.ShouldBe("TestRepo");
        result.Type.ShouldBe(EGitInfoType.CIBuild);
        result.ProjectName.ShouldBe("TestProject");
    }

    [Test]
    public void ExtractInfo_WithManualBuildEnvironment_ShouldReturnGitInfo()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "ghi789jkl012",
            ["BUILD_REPOSITORY_NAME"] = "ManualRepo",
            ["BUILD_REASON"] = "Manual",
            ["BUILD_BUILDID"] = "789",
            ["SYSTEM_TEAMPROJECT"] = "ManualProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(789);
        result.CommitId.ShouldBe("ghi789jkl012");
        result.RepositoryName.ShouldBe("ManualRepo");
        result.Type.ShouldBe(EGitInfoType.ManualBuild);
        result.ProjectName.ShouldBe("ManualProject");
    }

    [Test]
    public void ExtractInfo_WithUnknownBuildReason_ShouldReturnUnknownType()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "jkl012mno345",
            ["BUILD_REPOSITORY_NAME"] = "UnknownRepo",
            ["BUILD_REASON"] = "UnknownReason",
            ["BUILD_BUILDID"] = "999",
            ["SYSTEM_TEAMPROJECT"] = "UnknownProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.Type.ShouldBe(EGitInfoType.Unknown);
    }

    [Test]
    public void ExtractInfo_WithInvalidBuildId_ShouldUseNegativeOne()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "mno345pqr678",
            ["BUILD_REPOSITORY_NAME"] = "InvalidBuildRepo",
            ["BUILD_REASON"] = "IndividualCI",
            ["BUILD_BUILDID"] = "invalid",
            ["SYSTEM_TEAMPROJECT"] = "InvalidProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(-1);
    }

    [Test]
    public void ExtractInfo_WithMissingBuildId_ShouldUseNegativeOne()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "pqr678stu901",
            ["BUILD_REPOSITORY_NAME"] = "MissingBuildRepo",
            ["BUILD_REASON"] = "IndividualCI",
            ["SYSTEM_TEAMPROJECT"] = "MissingProject"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.PullRequestId.ShouldBe(-1);
    }

    [Test]
    public void ExtractInfo_WithMissingCommitId_ShouldReturnNull()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_REPOSITORY_NAME"] = "MissingCommitRepo",
            ["BUILD_REASON"] = "IndividualCI",
            ["BUILD_BUILDID"] = "123"
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
            ["BUILD_SOURCEVERSION"] = "stu901vwx234",
            ["BUILD_REASON"] = "IndividualCI",
            ["BUILD_BUILDID"] = "123"
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
            ["BUILD_SOURCEVERSION"] = "",
            ["BUILD_REPOSITORY_NAME"] = ""
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
            ["BUILD_SOURCEVERSION"] = "   ",
            ["BUILD_REPOSITORY_NAME"] = "   "
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractInfo_WithoutProjectName_ShouldReturnNullProjectName()
    {
        // Arrange
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["BUILD_SOURCEVERSION"] = "vwx234yza567",
            ["BUILD_REPOSITORY_NAME"] = "NoProjectRepo",
            ["BUILD_REASON"] = "IndividualCI",
            ["BUILD_BUILDID"] = "123"
        });

        // Act
        var result = _extractor.ExtractInfo();

        // Assert
        _ = result.ShouldNotBeNull();
        result!.ProjectName.ShouldBeNull();
    }
}
