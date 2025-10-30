using System.Text.Json;
using Lintellect.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.UnitTests.Infrastructure.Persistence.Configurations;

[TestFixture]
public sealed class AnalysisJobConfigurationTests
{
    [Test]
    public void AnalysisJobConfiguration_SanitizesSensitiveData_WhenSavingToDatabase()
    {
        // Arrange
        var originalRequest = new AnalysisRequest
        {
            Language = EProgrammingLanguage.CSharp,
            GitProvider = EGitProvider.AzureDevops,
            AccessToken = "sensitive-token",
            AzureDevOpsOrgUrl = "https://dev.azure.com/sensitive-org",
            GitInfo = new GitInfo(123, "commit123", "owner/repo"),
            Findings =
            [
                new() { RuleId = "RULE001", Message = "Test message", FilePath = "test.cs", Line = 1 }
            ]
        };

        var analysisRequestJson = JsonDocument.Parse(JsonSerializer.Serialize(originalRequest));

        // Act - Simulate EF Core conversion
        var configuration = new AnalysisJobConfiguration();
        var sanitizedJson = CallSanitizeMethod(configuration, analysisRequestJson);

        // Assert
        sanitizedJson.ShouldNotBeNull();
        sanitizedJson.ShouldNotContain("sensitive-token");
        sanitizedJson.ShouldNotContain("sensitive-org");
        sanitizedJson.ShouldNotContain("AccessToken");
        sanitizedJson.ShouldNotContain("AzureDevOpsOrgUrl");

        // Verify non-sensitive data is preserved
        sanitizedJson.ShouldContain("CSharp");
        sanitizedJson.ShouldContain("AzureDevops");
        sanitizedJson.ShouldContain("RULE001");
        sanitizedJson.ShouldContain("commit123");
    }

    [Test]
    public void AnalysisJobConfiguration_HandlesNullInput_Gracefully()
    {
        // Arrange
        var configuration = new AnalysisJobConfiguration();

        // Act
        var result = CallSanitizeMethod(configuration, null);

        // Assert
        result.ShouldBe("{}");
    }

    [Test]
    public void AnalysisJobConfiguration_HandlesInvalidJson_Gracefully()
    {
        // Arrange
        var invalidJson = JsonDocument.Parse("{\"invalid\": \"json\"}");
        var configuration = new AnalysisJobConfiguration();

        // Act
        var result = CallSanitizeMethod(configuration, invalidJson);

        // Assert
        // When invalid JSON is provided, it creates a default SanitizedAnalysisRequest
        // which serializes to a valid object with default values
        result.ShouldNotBeNull();
        result.ShouldContain("Language");
        result.ShouldContain("Findings");
        result.ShouldNotContain("AccessToken");
        result.ShouldNotContain("AzureDevOpsOrgUrl");
    }

    /// <summary>
    /// Uses reflection to call the private SanitizeAnalysisRequest method
    /// </summary>
    private static string? CallSanitizeMethod(AnalysisJobConfiguration configuration, JsonDocument? input)
    {
        var method = typeof(AnalysisJobConfiguration).GetMethod("SanitizeAnalysisRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull("SanitizeAnalysisRequest method should exist");

        return method.Invoke(null, [input]) as string;
    }
}
