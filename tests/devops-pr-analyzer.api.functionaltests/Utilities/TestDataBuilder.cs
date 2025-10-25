using Bogus;

namespace devops_pr_analyzer.api.functionaltests.Utilities;

/// <summary>
/// Fluent builder for creating test data.
/// </summary>
public sealed class TestDataBuilder
{
    private readonly Faker _faker = new();
    private readonly AnalysisRequest _request = new();

    public TestDataBuilder WithValidGitInfo()
    {
        _request.GitInfo = new GitInfo(_faker.UniqueIndex,
            _faker.Random.AlphaNumeric(40),
            _faker.Lorem.Word(),
            EGitInfoType.PullRequest,
            "TestProject");

        return this;
    }

    public TestDataBuilder WithGitProvider(EGitProvider provider)
    {
        _request.GitProvider = provider;
        return this;
    }

    public TestDataBuilder WithLanguage(EProgrammingLanguage language)
    {
        _request.Language = language;
        return this;
    }

    public TestDataBuilder WithGitHubCredentials()
    {
        _request.GitHubToken = "mock-github-token";
        return this;
    }

    public TestDataBuilder WithAzureDevOpsCredentials()
    {
        _request.DevopsPat = "mock-pat";
        _request.AzureDevOpsOrgUrl = "https://dev.azure.com/testorg";
        return this;
    }

    public TestDataBuilder WithEnabledFeatures()
    {
        _request.EnableSummaryComment = true;
        _request.EnableDescriptionSummary = true;
        _request.EnableInlineSuggestions = true;
        _request.EnableAzureDevopsCodeOwners = true;
        return this;
    }

    public TestDataBuilder WithFindings()
    {
        _request.Findings =
        [
            new()
            {
                RuleId = "CS0618",
                Message = "Test warning",
                FilePath = "TestFile.cs",
                Line = 10,
                Severity = "Warning"
            }
        ];
        return this;
    }

    public TestDataBuilder WithInvalidGitInfo()
    {
        _request.GitInfo = new GitInfo(
            -1,
            "",
            "TestRepo"
        );

        return this;
    }

    public TestDataBuilder WithNoCredentials()
    {
        _request.GitHubToken = null;
        _request.DevopsPat = null;
        _request.AzureDevOpsOrgUrl = null;
        return this;
    }

    public TestDataBuilder WithNoFeaturesEnabled()
    {
        _request.EnableSummaryComment = false;
        _request.EnableDescriptionSummary = false;
        _request.EnableInlineSuggestions = false;
        _request.EnableAzureDevopsCodeOwners = false;
        return this;
    }

    public AnalysisRequest Build()
    {
        return _request;
    }

    public static AnalysisRequest ValidRequest()
    {
        return new TestDataBuilder()
            .WithValidGitInfo()
            .WithGitProvider(EGitProvider.GitHub)
            .WithLanguage(EProgrammingLanguage.CSharp)
            .WithGitHubCredentials()
            .WithEnabledFeatures()
            .WithFindings()
            .Build();
    }

    public static AnalysisRequest InvalidRequest()
    {
        return new TestDataBuilder()
            .WithInvalidGitInfo()
            .WithGitProvider(EGitProvider.GitHub)
            .WithLanguage(EProgrammingLanguage.CSharp)
            .WithNoCredentials()
            .WithNoFeaturesEnabled()
            .Build();
    }
}
