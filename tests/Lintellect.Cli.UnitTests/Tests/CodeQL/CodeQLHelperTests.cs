using System.Reflection;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests.CodeQL;

[TestFixture]
public class CodeQLHelperTests : CodeQLTestBase
{
    [Test]
    public void TryValidateDatabasePath_WithValidSolutionPath_ShouldCreateCorrectPaths()
    {
        // Arrange
        var solutionPath = Path.Combine(Path.GetTempPath(), "TestSolution.sln");
        var expectedSolutionDir = Path.GetDirectoryName(solutionPath);
        var expectedDatabasePath = Path.Combine(expectedSolutionDir!, "codeql-database");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath, null!, null! };
        method?.Invoke(null, parameters);
        var solutionDir = (string)parameters[1];
        var databasePath = (string)parameters[2];

        // Assert
        solutionDir.ShouldBe(expectedSolutionDir);
        databasePath.ShouldBe(expectedDatabasePath);
    }

    [Test]
    public void TryValidateDatabasePath_WithExistingDatabase_ShouldCleanUpDirectory()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var solutionPath = Path.Combine(tempDir, "TestSolution.sln");
        var databasePath = Path.Combine(tempDir, "codeql-database");

        // Create existing database directory
        Directory.CreateDirectory(databasePath);
        File.WriteAllText(Path.Combine(databasePath, "test.txt"), "test");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath, null!, null! };
        method?.Invoke(null, parameters);
        var resultDatabasePath = (string)parameters[2];

        // Assert
        resultDatabasePath.ShouldBe(databasePath);
        Directory.Exists(databasePath).ShouldBeFalse("Existing database directory should be cleaned up");
    }

    [Test]
    public void TryValidateDatabasePath_WithEmptySolutionPath_ShouldUseCurrentDirectory()
    {
        // Arrange
        var solutionPath = "";
        var expectedSolutionDir = ".";
        var expectedDatabasePath = Path.Combine(".", "codeql-database");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath, null!, null! };
        method?.Invoke(null, parameters);
        var solutionDir = (string)parameters[1];
        var databasePath = (string)parameters[2];

        // Assert
        solutionDir.ShouldBe(expectedSolutionDir);
        databasePath.ShouldBe(expectedDatabasePath);
    }

    [Test]
    public void TryValidateDatabasePath_WithNullSolutionPath_ShouldUseCurrentDirectory()
    {
        // Arrange
        string? solutionPath = null;
        var expectedSolutionDir = ".";
        var expectedDatabasePath = Path.Combine(".", "codeql-database");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath!, null!, null! };
        method?.Invoke(null, parameters);
        var solutionDir = (string)parameters[1];
        var databasePath = (string)parameters[2];

        // Assert
        solutionDir.ShouldBe(expectedSolutionDir);
        databasePath.ShouldBe(expectedDatabasePath);
    }

    [Test]
    public void TryValidateDatabasePath_WithComplexPath_ShouldHandleCorrectly()
    {
        // Arrange
        var solutionPath = Path.Combine("Projects", "MyApp", "src", "MyApp.sln");
        var expectedSolutionDir = Path.Combine("Projects", "MyApp", "src");
        var expectedDatabasePath = Path.Combine(expectedSolutionDir, "codeql-database");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath, null!, null! };
        method?.Invoke(null, parameters);
        var solutionDir = (string)parameters[1];
        var databasePath = (string)parameters[2];

        // Assert
        solutionDir.ShouldBe(expectedSolutionDir);
        databasePath.ShouldBe(expectedDatabasePath);
    }

    [Test]
    public void TryValidateDatabasePath_WithRelativePath_ShouldHandleCorrectly()
    {
        // Arrange
        var solutionPath = "MyApp.sln";
        var expectedSolutionDir = Path.GetDirectoryName(solutionPath) ?? ".";
        var expectedDatabasePath = Path.Combine(expectedSolutionDir, "codeql-database");

        // Act
        var method = typeof(CodeQLAnalyzerBase).GetMethod("TryValidateDatabasePath", BindingFlags.NonPublic | BindingFlags.Static);
        var parameters = new object[] { solutionPath, null!, null! };
        method?.Invoke(null, parameters);
        var solutionDir = (string)parameters[1];
        var databasePath = (string)parameters[2];

        // Assert
        solutionDir.ShouldBe(expectedSolutionDir);
        databasePath.ShouldBe(expectedDatabasePath);
    }
}
