using Bogus;

namespace Lintellect.Api.FunctionalTests.Utilities.Analysis;

/// <summary>
/// Fluent builder for creating analysis test data.
/// </summary>
public sealed class TestDataBuilder
{
    private readonly Faker _faker = new();
    private readonly AnalysisRequest _request = new();

    public TestDataBuilder WithValidGitInfo()
    {
        _request.GitInfo = new GitInfo(
            _faker.Random.Int(1, 10000), // Valid PR ID
            _faker.Random.AlphaNumeric(40), // Commit SHA
            "TestRepo", // Repository name
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

    public TestDataBuilder WithPullRequestId(int pullRequestId)
    {
        if (_request.GitInfo != null)
        {
            _request.GitInfo = _request.GitInfo with { PullRequestId = pullRequestId };
        }
        return this;
    }

    public TestDataBuilder WithRepositoryName(string repositoryName)
    {
        if (_request.GitInfo != null)
        {
            _request.GitInfo = _request.GitInfo with { RepositoryName = repositoryName };
        }
        return this;
    }

    public TestDataBuilder WithProjectName(string projectName)
    {
        if (_request.GitInfo != null)
        {
            _request.GitInfo = _request.GitInfo with { ProjectName = projectName };
        }
        return this;
    }

    public static AnalysisRequest ValidRequest()
    {
        return new TestDataBuilder()
            .WithValidGitInfo()
            .WithPullRequestId(123) // Use a consistent PR ID for tests
            .WithRepositoryName("TestRepo")
            .WithProjectName("TestProject")
            .WithGitProvider(EGitProvider.GitHub)
            .WithLanguage(EProgrammingLanguage.CSharp)
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
            .WithNoFeaturesEnabled()
            .Build();
    }
}

