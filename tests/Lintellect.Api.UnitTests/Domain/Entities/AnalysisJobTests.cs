using System.Text.Json;

namespace Lintellect.Api.UnitTests.Domain.Entities;

[TestFixture]
public sealed class AnalysisJobTests
{
    [Test]
    public void AnalysisJob_Constructor_StoresOriginalData()
    {
        // Arrange
        var originalRequest = new AnalysisRequest
        {
            Language = EProgrammingLanguage.CSharp,
            GitProvider = EGitProvider.AzureDevops,
            DevopsPat = "sensitive-pat-token",
            AzureDevOpsOrgUrl = "https://dev.azure.com/sensitive-org",
            GitHubToken = "sensitive-github-token",
            GitInfo = new GitInfo(123, "commit123", "owner/repo"),
            Findings =
            [
                new() { RuleId = "RULE001", Message = "Test message", FilePath = "test.cs", Line = 1 }
            ]
        };

        // Act
        var analysisJob = new AnalysisJob(originalRequest);

        // Assert
        analysisJob.AnalysisRequest.ShouldNotBeNull();

        // The domain entity stores the original data - sanitization happens at the EF Core level
        var jsonString = analysisJob.AnalysisRequest.RootElement.GetRawText();
        jsonString.ShouldContain("sensitive-pat-token");
        jsonString.ShouldContain("sensitive-github-token");
        jsonString.ShouldContain("sensitive-org");
        jsonString.ShouldContain("DevopsPat");
        jsonString.ShouldContain("GitHubToken");
        jsonString.ShouldContain("AzureDevOpsOrgUrl");
    }

    [Test]
    public void SanitizedAnalysisRequest_FromAnalysisRequest_ExcludesSensitiveFields()
    {
        // Arrange
        var originalRequest = new AnalysisRequest
        {
            Language = EProgrammingLanguage.Python,
            GitProvider = EGitProvider.GitHub,
            DevopsPat = "secret-pat",
            AzureDevOpsOrgUrl = "https://dev.azure.com/secret",
            GitHubToken = "secret-token",
            EnableSummaryComment = true,
            EnableInlineSuggestions = false
        };

        // Act
        var sanitized = SanitizedAnalysisRequest.FromAnalysisRequest(originalRequest);

        // Assert
        sanitized.Language.ShouldBe(EProgrammingLanguage.Python);
        sanitized.GitProvider.ShouldBe(EGitProvider.GitHub);
        sanitized.EnableSummaryComment.ShouldBeTrue();
        sanitized.EnableInlineSuggestions.ShouldBeFalse();

        // Verify sensitive fields are not accessible
        var jsonString = JsonSerializer.Serialize(sanitized);
        jsonString.ShouldNotContain("secret-pat");
        jsonString.ShouldNotContain("secret-token");
        jsonString.ShouldNotContain("secret");
    }

}
