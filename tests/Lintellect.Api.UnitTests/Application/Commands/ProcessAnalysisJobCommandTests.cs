using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.UnitTests.Application.Commands;

[TestFixture]
public class ProcessAnalysisJobCommandTests
{
    private IApplicationDbContext _mockContext = null!;
    private IGitClient _mockGitClient = null!;
    private IAnalyzerService _mockAnalyzer = null!;
    private ProcessAnalysisJobCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = Substitute.For<IApplicationDbContext>();

        _mockGitClient = Substitute.For<IGitClient>();
        _mockGitClient.GetPullRequestCompactDiffsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new Dictionary<string, string> { ["File.cs"] = "diff content" });
        _mockGitClient.GetPullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(new PullRequest { PullRequestId = 123, SourceRefName = "refs/heads/feature" });
        _mockGitClient.CreateCommentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>())
            .Returns(new PullRequestCommentThread { Id = 1, Comments = [] });

        var gitClientFactory = Substitute.For<IGitClientFactory>();
        gitClientFactory.CreateClient(Arg.Any<AnalysisRequest>()).Returns(_mockGitClient);
        var prService = new PullRequestService(gitClientFactory);

        _mockAnalyzer = Substitute.For<IAnalyzerService>();
        _mockAnalyzer.GetDetailedAnalysisAsync(Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("Detailed analysis text");
        _mockAnalyzer.GenerateInlineSuggestionsAsync(Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var workItemService = Substitute.For<IWorkItemService>();
        var analysisOptions = Options.Create(new AnalysisOptions());
        var logger = Substitute.For<ILogger<ProcessAnalysisJobCommandHandler>>();

        _handler = new ProcessAnalysisJobCommandHandler(
            _mockContext, prService, _mockAnalyzer, workItemService, analysisOptions, logger);
    }

    private static AnalysisRequest ScopedRequest()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableSummaryComment = true;
        request.EnableDescriptionSummary = false;
        request.EnableInlineSuggestions = false;
        request.EnableAzureDevopsCodeOwners = false;
        request.EnableWorkItemContext = false;
        return request;
    }

    [Test]
    public async Task PostResultsToPullRequestAsync_WhenInitialThreadIdStored_PassesThreadIdAndResolvedTrueToAddComment()
    {
        // Arrange
        var job = new AnalysisJobBuilder()
            .WithAnalysisRequest(ScopedRequest())
            .WithInitialCommentThreadId(7)
            .Build();
        var jobsDbSet = new[] { job }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(jobsDbSet);

        var command = new ProcessAnalysisJobCommand(job.Id, job.CreateAnalysisRequestSnapshot());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockGitClient.Received(1).CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), "Detailed analysis text", 7, true);
    }

    [Test]
    public async Task PostResultsToPullRequestAsync_WhenNoInitialThreadIdStored_CreatesNewCommentAsToday()
    {
        // Arrange
        var job = new AnalysisJobBuilder()
            .WithAnalysisRequest(ScopedRequest())
            .Build();
        var jobsDbSet = new[] { job }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(jobsDbSet);

        var command = new ProcessAnalysisJobCommand(job.Id, job.CreateAnalysisRequestSnapshot());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockGitClient.Received(1).CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), "Detailed analysis text", null, true);
    }

    private static AnalysisRequest InlineOnlyRequest()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        request.EnableSummaryComment = false;
        request.EnableDescriptionSummary = false;
        request.EnableInlineSuggestions = true;
        request.EnableAzureDevopsCodeOwners = false;
        request.EnableWorkItemContext = false;
        return request;
    }

    [Test]
    public async Task Handle_WithReanalysisBaseCommit_UsesIncrementalDiffAndOnlyRunsInlineSuggestions()
    {
        var job = new AnalysisJobBuilder().WithAnalysisRequest(InlineOnlyRequest()).Build();
        var jobsDbSet = new[] { job }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(jobsDbSet);
        _mockGitClient.GetCompactDiffsBetweenCommitsAsync(
                Arg.Any<string>(), Arg.Any<string>(), "base-1", "head-1", Arg.Any<int>())
            .Returns(new Dictionary<string, string> { ["File.cs"] = "incremental diff" });

        var command = new ProcessAnalysisJobCommand(job.Id, job.CreateAnalysisRequestSnapshot(), "head-1", "base-1");
        await _handler.Handle(command, CancellationToken.None);

        await _mockGitClient.Received(1).GetCompactDiffsBetweenCommitsAsync(
            Arg.Any<string>(), Arg.Any<string>(), "base-1", "head-1", Arg.Any<int>());
        await _mockGitClient.DidNotReceive().GetPullRequestCompactDiffsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
        await _mockAnalyzer.Received(1).GenerateInlineSuggestionsAsync(
            Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
        await _mockAnalyzer.DidNotReceive().GetDetailedAnalysisAsync(
            Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
        await _mockAnalyzer.DidNotReceive().GenerateSummaryAsync(
            Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenIncrementalDiffFails_FallsBackToFullPrDiff()
    {
        var job = new AnalysisJobBuilder().WithAnalysisRequest(InlineOnlyRequest()).Build();
        var jobsDbSet = new[] { job }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(jobsDbSet);
        _mockGitClient.GetCompactDiffsBetweenCommitsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Task.FromException<Dictionary<string, string>>(new InvalidOperationException("base commit gone")));

        var command = new ProcessAnalysisJobCommand(job.Id, job.CreateAnalysisRequestSnapshot(), "head-1", "base-1");
        await _handler.Handle(command, CancellationToken.None);

        await _mockGitClient.Received(1).GetPullRequestCompactDiffsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
        await _mockAnalyzer.Received(1).GenerateInlineSuggestionsAsync(
            Arg.Any<AnalyzerServiceModel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutReanalysisBaseCommit_UsesFullPrDiff()
    {
        var job = new AnalysisJobBuilder().WithAnalysisRequest(ScopedRequest()).Build();
        var jobsDbSet = new[] { job }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(jobsDbSet);

        var command = new ProcessAnalysisJobCommand(job.Id, job.CreateAnalysisRequestSnapshot());
        await _handler.Handle(command, CancellationToken.None);

        await _mockGitClient.DidNotReceive().GetCompactDiffsBetweenCommitsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        await _mockGitClient.Received().GetPullRequestCompactDiffsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }
}
