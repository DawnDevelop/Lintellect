namespace Lintellect.Api.UnitTests.Builders;

/// <summary>
/// Test data builder for AnalysisJob entity.
/// </summary>
public sealed class AnalysisJobBuilder
{
    private int? _initialCommentThreadId;

    private AnalysisRequest _analysisRequest = new()
    {
        GitInfo = new GitInfo(123, "commit123", "TestRepo", EGitInfoType.PullRequest, "TestProject"),
        Language = EProgrammingLanguage.CSharp,
        GitProvider = EGitProvider.GitHub,
        EnableSummaryComment = true,
        EnableInitialComment = true,
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

    public AnalysisJobBuilder WithAnalysisRequest(AnalysisRequest request)
    {
        _analysisRequest = request;
        return this;
    }

    public AnalysisJobBuilder WithGitInfo(GitInfo gitInfo)
    {
        _analysisRequest.GitInfo = gitInfo;
        return this;
    }

    public AnalysisJobBuilder WithLanguage(EProgrammingLanguage language)
    {
        _analysisRequest.Language = language;
        return this;
    }

    public AnalysisJobBuilder WithFindings(List<AnalyzerFindings> findings)
    {
        _analysisRequest.Findings = findings;
        return this;
    }

    public AnalysisJobBuilder WithInitialCommentThreadId(int threadId)
    {
        _initialCommentThreadId = threadId;
        return this;
    }

    public AnalysisJob Build()
    {
        var job = new AnalysisJob(_analysisRequest);

        if (_initialCommentThreadId.HasValue)
        {
            job.SetInitialCommentThreadId(_initialCommentThreadId.Value);
        }

        return job;
    }

    public static AnalysisJob ValidJob()
    {
        return new AnalysisJobBuilder().Build();
    }

    public static AnalysisJob JobWithLanguage(EProgrammingLanguage language)
    {
        return new AnalysisJobBuilder()
            .WithLanguage(language)
            .Build();
    }
}
