namespace Lintellect.Api.UnitTests.Builders;

/// <summary>
/// Test data builder for AnalysisRequest.
/// </summary>
public sealed class AnalysisRequestBuilder
{
    private readonly AnalysisRequest _request = new()
    {
        GitInfo = new GitInfo(123, "commit123", "TestRepo", EGitInfoType.PullRequest, "TestProject"),
        Language = EProgrammingLanguage.CSharp,
        GitProvider = EGitProvider.GitHub,
        AccessToken = "test-token",
        EnableSummaryComment = true,
        EnableDescriptionSummary = true,
        EnableInlineSuggestions = true,
        EnableAzureDevopsCodeOwners = true,
        Findings =
        [
            new()
            {
                RuleId = "CS0618",
                Message = "Test warning",
                FilePath = "TestFile.cs",
                Line = 10,
                Severity = "Warning"
            }
        ]
    };

    public AnalysisRequestBuilder WithGitInfo(GitInfo gitInfo)
    {
        _request.GitInfo = gitInfo;
        return this;
    }

    public AnalysisRequestBuilder WithProjectName(string projectName)
    {
        var currentGitInfo = _request.GitInfo!;
        _request.GitInfo = currentGitInfo with { ProjectName = projectName };
        return this;
    }

    public AnalysisRequestBuilder WithRepositoryName(string repositoryName)
    {
        var currentGitInfo = _request.GitInfo!;
        _request.GitInfo = currentGitInfo with { RepositoryName = repositoryName };
        return this;
    }

    public AnalysisRequestBuilder WithPullRequestId(int pullRequestId)
    {
        var currentGitInfo = _request.GitInfo!;
        _request.GitInfo = currentGitInfo with { PullRequestId = pullRequestId };
        return this;
    }

    public AnalysisRequestBuilder WithLanguage(EProgrammingLanguage language)
    {
        _request.Language = language;
        return this;
    }

    public AnalysisRequestBuilder WithGitProvider(EGitProvider provider)
    {
        _request.GitProvider = provider;
        return this;
    }

    public AnalysisRequestBuilder WithAccessToken(string token)
    {
        _request.AccessToken = token;
        return this;
    }

    public AnalysisRequestBuilder WithAzureDevOpsCredentials(string accessToken, string orgUrl)
    {
        _request.AccessToken = accessToken;
        _request.AzureDevOpsOrgUrl = orgUrl;
        return this;
    }

    public AnalysisRequestBuilder WithNoCredentials()
    {
        _request.AccessToken = null;
        _request.AzureDevOpsOrgUrl = null;
        return this;
    }

    public AnalysisRequestBuilder WithNoFeaturesEnabled()
    {
        _request.EnableSummaryComment = false;
        _request.EnableDescriptionSummary = false;
        _request.EnableInlineSuggestions = false;
        _request.EnableAzureDevopsCodeOwners = false;
        return this;
    }

    public AnalysisRequestBuilder WithFindings(List<AnalyzerFindings> findings)
    {
        _request.Findings = findings;
        return this;
    }

    public AnalysisRequest Build()
    {
        return _request;
    }

    public static AnalysisRequest ValidRequest()
    {
        return new AnalysisRequestBuilder().Build();
    }

    public static AnalysisRequest InvalidRequest()
    {
        return new AnalysisRequestBuilder()
            .WithProjectName("") // Invalid - empty
            .WithPullRequestId(0) // Invalid - zero
            .WithNoCredentials()
            .WithNoFeaturesEnabled()
            .Build();
    }
}
