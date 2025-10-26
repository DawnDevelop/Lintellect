using Lintellect.Shared.Extensions;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class ExclusionPatternTests
{
    [Test]
    public void FilePatternMatcher_ShouldExcludeFilesMatchingPatterns()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "src/Controllers/HomeController.cs",
            "bin/Debug/MyApp.exe",
            "obj/Debug/MyApp.dll",
            "tests/UnitTests.cs",
            "src/Models/User.cs"
        };

        var exclusionPatterns = new List<string>
        {
            "**/bin/**",
            "**/obj/**",
            "**/test*/**"
        };

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldContain("src/Controllers/HomeController.cs");
        filteredFiles.ShouldContain("src/Models/User.cs");

        filteredFiles.ShouldNotContain("bin/Debug/MyApp.exe");
        filteredFiles.ShouldNotContain("obj/Debug/MyApp.dll");
        filteredFiles.ShouldNotContain("tests/UnitTests.cs");

        filteredFiles.Count.ShouldBe(3);
    }

    [Test]
    public void FilePatternMatcher_ShouldHandleEmptyExclusionPatterns()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "bin/Debug/MyApp.exe"
        };

        var exclusionPatterns = new List<string>();

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.Count.ShouldBe(2);
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldContain("bin/Debug/MyApp.exe");
    }

    [Test]
    public void FilePatternMatcher_ShouldHandleNullExclusionPatterns()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "bin/Debug/MyApp.exe"
        };

        List<string>? exclusionPatterns = null;

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.Count.ShouldBe(2);
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldContain("bin/Debug/MyApp.exe");
    }

    [Test]
    public void FilePatternMatcher_ShouldExcludeSpecificFileExtensions()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "src/App.config",
            "src/Web.config",
            "src/package.json",
            "src/README.md"
        };

        var exclusionPatterns = new List<string>
        {
            "**/*.config",
            "**/*.json",
            "**/*.md"
        };

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldNotContain("src/App.config");
        filteredFiles.ShouldNotContain("src/Web.config");
        filteredFiles.ShouldNotContain("src/package.json");
        filteredFiles.ShouldNotContain("src/README.md");

        filteredFiles.Count.ShouldBe(1);
    }

    [Test]
    public void FilePatternMatcher_ShouldExcludeDirectoriesWithWildcards()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "src/Controllers/HomeController.cs",
            "src/Views/Home/Index.cshtml",
            "wwwroot/css/site.css",
            "wwwroot/js/site.js",
            "src/Models/User.cs"
        };

        var exclusionPatterns = new List<string>
        {
            "**/wwwroot/**",
            "**/Views/**"
        };

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldContain("src/Controllers/HomeController.cs");
        filteredFiles.ShouldContain("src/Models/User.cs");

        filteredFiles.ShouldNotContain("src/Views/Home/Index.cshtml");
        filteredFiles.ShouldNotContain("wwwroot/css/site.css");
        filteredFiles.ShouldNotContain("wwwroot/js/site.js");

        filteredFiles.Count.ShouldBe(3);
    }

    [Test]
    public void FilePatternMatcher_ShouldHandleComplexPatterns()
    {
        // Arrange
        var filePaths = new List<string>
        {
            "src/Program.cs",
            "src/Controllers/HomeController.cs",
            "src/Controllers/AdminController.cs",
            "src/Services/UserService.cs",
            "src/Services/AdminService.cs",
            "tests/UnitTests/UserServiceTests.cs",
            "tests/IntegrationTests/AdminControllerTests.cs"
        };

        var exclusionPatterns = new List<string>
        {
            "**/test*/**",
            "**/Admin*"
        };

        // Act
        var filteredFiles = FilePatternMatcher.FilterFiles(filePaths, exclusionPatterns).ToList();

        // Assert
        filteredFiles.ShouldContain("src/Program.cs");
        filteredFiles.ShouldContain("src/Controllers/HomeController.cs");
        filteredFiles.ShouldContain("src/Services/UserService.cs");

        filteredFiles.ShouldNotContain("src/Controllers/AdminController.cs");
        filteredFiles.ShouldNotContain("src/Services/AdminService.cs");
        filteredFiles.ShouldNotContain("tests/UnitTests/UserServiceTests.cs");
        filteredFiles.ShouldNotContain("tests/IntegrationTests/AdminControllerTests.cs");

        filteredFiles.Count.ShouldBe(3);
    }
}
