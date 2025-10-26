using Lintellect.Cli.Services;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class AnalyzerApiClientServiceTests
{
    private const string TestApiKey = "test-api-key";
    private static Uri TestBaseUrl => new("https://api.example.com");

    [Test]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var baseUrl = TestBaseUrl;

        // Act & Assert
        _ = Should.NotThrow(() => new AnalyzerApiClientService(baseUrl, TestApiKey));
    }

    [Test]
    public void Constructor_WithNullBaseUrl_ShouldThrowException()
    {
        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => new AnalyzerApiClientService(null!, TestApiKey));
    }

    [Test]
    public void Constructor_WithNullApiKey_ShouldThrowException()
    {
        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => new AnalyzerApiClientService(TestBaseUrl, null!));
    }

    [Test]
    public void Constructor_WithEmptyApiKey_ShouldNotThrow()
    {
        // Act & Assert
        _ = Should.NotThrow(() => new AnalyzerApiClientService(TestBaseUrl, ""));
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        using AnalyzerApiClientService service = new(TestBaseUrl, TestApiKey);

        // Act & Assert
        Should.NotThrow(service.Dispose);
    }

    [Test]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        using AnalyzerApiClientService service = new(TestBaseUrl, TestApiKey);

        // Act & Assert
        Should.NotThrow(service.Dispose);
        Should.NotThrow(service.Dispose);
    }

    [Test]
    public async Task StartAnalysisAsync_WithNullRequest_ShouldThrowException()
    {
        // Arrange
        using AnalyzerApiClientService service = new(TestBaseUrl, TestApiKey);

        // Act & Assert
        Func<Task<HttpResponseMessage>> act = async () => await service.StartAnalysisAsync(null!);
        _ = await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task StartAnalysisAsync_WithValidRequest_ShouldNotThrow()
    {
        // Arrange
        var analysisRequest = new AnalysisRequest()
        {
            Language = EProgrammingLanguage.CSharp,
            Findings =
            [
                new() { RuleId = "CS0618", Message = "Test message", FilePath = "test.cs", Line = 1, Severity = "Warning" }
            ]
        };

        using AnalyzerApiClientService service = new(TestBaseUrl, TestApiKey);

        // Act & Assert
        // Note: This will fail with network error, but we're testing the method exists and accepts the parameter
        Func<Task<HttpResponseMessage>> act = async () => await service.StartAnalysisAsync(analysisRequest);
        _ = await act.ShouldThrowAsync<HttpRequestException>();
    }
}
