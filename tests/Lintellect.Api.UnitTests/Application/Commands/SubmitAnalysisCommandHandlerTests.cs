using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace Lintellect.Api.UnitTests.Application.Commands;

[TestFixture]
public class SubmitAnalysisCommandHandlerTests
{
    private const string CurrentSourceHead = "head-current";

    private IApplicationDbContext _mockContext = null!;
    private AnalysisJobQueue _queue = null!;
    private IGitClient _mockGitClient = null!;
    private ILogger<SubmitAnalysisCommandHandler> _mockLogger = null!;
    private SubmitAnalysisCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockContext = Substitute.For<IApplicationDbContext>();
        var mockDbSet = Array.Empty<AnalysisJob>().ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        _queue = new AnalysisJobQueue();

        _mockGitClient = Substitute.For<IGitClient>();
        _mockGitClient.GetPullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(new PullRequest
            {
                PullRequestId = 123,
                SourceCommit = new CommitRef { CommitId = CurrentSourceHead }
            });
        var mockGitClientFactory = Substitute.For<IGitClientFactory>();
        mockGitClientFactory.CreateClient(Arg.Any<AnalysisRequest>()).Returns(_mockGitClient);
        var prService = new PullRequestService(mockGitClientFactory);

        _mockLogger = Substitute.For<ILogger<SubmitAnalysisCommandHandler>>();
        _handler = new SubmitAnalysisCommandHandler(_mockContext, _queue, prService, _mockLogger);
    }

    private static AnalysisJob CompletedJob(string? sourceCommitId)
    {
        var job = new AnalysisJob(AnalysisRequestBuilder.ValidRequest(), sourceCommitId);
        job.Start();
        job.Complete("summary", "detailed", null, "TestAnalyzer");
        return job;
    }

    [Test]
    public async Task Handle_WithValidCommand_CreatesJobAndEnqueues()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockContext.SaveChangesAsync(cancellationToken)
            .Returns(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);

        _mockContext.AnalysisJobs.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Status == AnalysisStatus.Pending &&
            j.AnalysisRequest != null));
        await _mockContext.Received(1).SaveChangesAsync(cancellationToken);

        // Verify that the job was enqueued by checking if we can dequeue it
        var dequeuedJob = await _queue.DequeueAsync(cancellationToken);
        dequeuedJob.ShouldNotBeNull();
        dequeuedJob.Id.ShouldBe(result);
    }

    [Test]
    public async Task Handle_WithValidCommand_ReturnsJobId()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockContext.SaveChangesAsync(cancellationToken)
            .Returns(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        Guid.TryParse(result.ToString(), out _).ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithEnableInitialCommentAndEnableSummaryCommentTrue_PostsPlaceholderAndStoresThreadId()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = true;
        request.EnableSummaryComment = true;
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockGitClient.CreateCommentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>())
            .Returns(new PullRequestCommentThread { Id = 42, Comments = [] });

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        await _mockGitClient.Received(1).CreateCommentAsync(
            request.GitInfo!.ProjectName!, request.GitInfo.RepositoryName, request.GitInfo.PullRequestId,
            Arg.Any<string>(), null, false);

        _mockContext.AnalysisJobs.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Id == result && j.InitialCommentThreadId == 42));
    }

    [Test]
    public async Task Handle_WithEnableInitialCommentFalse_DoesNotPostPlaceholder()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        request.EnableSummaryComment = true;
        var command = new SubmitAnalysisCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockGitClient.DidNotReceive().CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>());

        _mockContext.AnalysisJobs.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Id == result && j.InitialCommentThreadId == null));
    }

    [Test]
    public async Task Handle_WithEnableSummaryCommentFalseButInitialCommentTrue_DoesNotPostPlaceholder()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = true;
        request.EnableSummaryComment = false;
        var command = new SubmitAnalysisCommand(request);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockGitClient.DidNotReceive().CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Handle_WhenPlaceholderPostingThrows_LogsErrorAndStillEnqueuesJob()
    {
        // Arrange
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = true;
        request.EnableSummaryComment = true;
        var command = new SubmitAnalysisCommand(request);
        var cancellationToken = CancellationToken.None;

        _mockGitClient.CreateCommentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>())
            .Returns(Task.FromException<PullRequestCommentThread>(new InvalidOperationException("boom")));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.ShouldNotBe(Guid.Empty);

        _mockContext.AnalysisJobs.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Id == result && j.InitialCommentThreadId == null));
        await _mockContext.Received(1).SaveChangesAsync(cancellationToken);

        var dequeuedJob = await _queue.DequeueAsync(cancellationToken);
        dequeuedJob.ShouldNotBeNull();
        dequeuedJob.Id.ShouldBe(result);
    }

    [Test]
    public async Task Handle_WhenPreviousJobStillPending_ReturnsExistingJobIdWithoutCreatingNewJob()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        var existingJob = new AnalysisJobBuilder().Build();
        var mockDbSet = new[] { existingJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        var command = new SubmitAnalysisCommand(request);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldBe(existingJob.Id);
        mockDbSet.DidNotReceive().Add(Arg.Any<AnalysisJob>());
        await _mockContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockGitClient.DidNotReceive().CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>());
        _queue.Reader.Count.ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenPreviousJobForPullRequestFailed_CreatesNewJob()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        var failedJob = new AnalysisJobBuilder().Build();
        failedJob.Fail("previous run failed");
        var mockDbSet = new[] { failedJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        var command = new SubmitAnalysisCommand(request);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldNotBe(failedJob.Id);
        mockDbSet.Received(1).Add(Arg.Is<AnalysisJob>(j => j.Id == result));
        await _mockContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPreviousJobCompletedAndNewCommitsPushed_QueuesInlineOnlyReanalysis()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        var previousJob = CompletedJob("head-old");
        var mockDbSet = new[] { previousJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        var command = new SubmitAnalysisCommand(request);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldNotBe(previousJob.Id);
        mockDbSet.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Id == result &&
            j.SourceCommitId == CurrentSourceHead &&
            j.ReanalysisBaseCommitId == "head-old"));
        request.EnableInlineSuggestions.ShouldBeTrue();
        request.EnableSummaryComment.ShouldBeFalse();
        request.EnableDescriptionSummary.ShouldBeFalse();
        request.EnableInitialComment.ShouldBeFalse();
        request.EnableAzureDevopsCodeOwners.ShouldBeFalse();
        await _mockGitClient.DidNotReceive().CreateCommentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Handle_WhenPreviousJobCompletedForSameSourceHead_ReturnsExistingJobIdWithoutCreatingNewJob()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        var previousJob = CompletedJob(CurrentSourceHead);
        var mockDbSet = new[] { previousJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);

        var result = await _handler.Handle(new SubmitAnalysisCommand(request), CancellationToken.None);

        result.ShouldBe(previousJob.Id);
        mockDbSet.DidNotReceive().Add(Arg.Any<AnalysisJob>());
        await _mockContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPreviousJobCompletedAndInlineSuggestionsDisabled_ReturnsExistingJobIdWithoutCreatingNewJob()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInlineSuggestions = false;
        var previousJob = CompletedJob("head-old");
        var mockDbSet = new[] { previousJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);

        var result = await _handler.Handle(new SubmitAnalysisCommand(request), CancellationToken.None);

        result.ShouldBe(previousJob.Id);
        mockDbSet.DidNotReceive().Add(Arg.Any<AnalysisJob>());
    }

    [Test]
    public async Task Handle_WhenSourceHeadResolutionFails_StillCreatesJobWithoutSourceCommit()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        _mockGitClient.GetPullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Task.FromException<PullRequest>(new InvalidOperationException("provider unavailable")));

        var result = await _handler.Handle(new SubmitAnalysisCommand(request), CancellationToken.None);

        result.ShouldNotBe(Guid.Empty);
        _mockContext.AnalysisJobs.Received(1).Add(Arg.Is<AnalysisJob>(j =>
            j.Id == result && j.SourceCommitId == null));
    }

    [Test]
    public async Task Handle_WhenExistingJobIsForDifferentPullRequest_CreatesNewJob()
    {
        var request = AnalysisRequestBuilder.ValidRequest();
        request.EnableInitialComment = false;
        var otherPrJob = new AnalysisJobBuilder()
            .WithAnalysisRequest(new AnalysisRequestBuilder().WithPullRequestId(999).Build())
            .Build();
        var mockDbSet = new[] { otherPrJob }.ToMockDbSet();
        _mockContext.AnalysisJobs.Returns(mockDbSet);
        var command = new SubmitAnalysisCommand(request);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldNotBe(otherPrJob.Id);
        mockDbSet.Received(1).Add(Arg.Is<AnalysisJob>(j => j.Id == result));
    }
}
