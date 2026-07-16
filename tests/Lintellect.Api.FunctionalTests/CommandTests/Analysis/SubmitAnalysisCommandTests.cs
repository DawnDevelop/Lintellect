using Lintellect.Api.Application.Common.Exceptions;
using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.FunctionalTests.Mocks.Git;
using Microsoft.EntityFrameworkCore;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class SubmitAnalysisCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithValidCommand_CreatesJobAndEnqueues()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        var jobId = await SendAsync(command);

        // Assert
        jobId.ShouldNotBe(Guid.Empty);

        // Verify job was created in database
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);

            job.ShouldNotBeNull();
            job!.Status.ShouldBe(AnalysisStatus.Pending);
            job.AnalysisRequest.ShouldNotBeNull();
        }
    }

    [Test]
    public async Task Handle_WhenJobForSamePullRequestAlreadyExists_ReturnsExistingJobIdWithoutCreatingSecondJob()
    {
        var firstJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));

        var secondJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));

        secondJobId.ShouldBe(firstJobId);

        var (scope, context) = GetDbContext();
        using (scope)
        {
            var jobCount = await context.AnalysisJobs.CountAsync();
            jobCount.ShouldBe(1);
        }
    }

    [Test]
    public async Task Handle_WhenPreviousJobCompletedAndNewCommitsPushed_QueuesInlineOnlyReanalysis()
    {
        var sharedClient = MockGitClientFactory.SharedClient;
        var originalSourceCommit = sharedClient.SourceCommitIdToReturn;
        try
        {
            sharedClient.SourceCommitIdToReturn = "commit-a";
            var firstJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));
            await SendAsync(new UpdateAnalysisJobStatusCommand(firstJobId, AnalysisStatus.Running, StartedAt: DateTimeOffset.UtcNow));
            await SendAsync(new CompleteAnalysisJobCommand(firstJobId, "summary", "detailed", null, "TestAnalyzer"));

            sharedClient.SourceCommitIdToReturn = "commit-b";
            var secondJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));

            secondJobId.ShouldNotBe(firstJobId);

            var (scope, context) = GetDbContext();
            using (scope)
            {
                var secondJob = await context.AnalysisJobs.FindAsync(secondJobId);

                secondJob.ShouldNotBeNull();
                secondJob!.SourceCommitId.ShouldBe("commit-b");
                secondJob.ReanalysisBaseCommitId.ShouldBe("commit-a");
                secondJob.AnalysisRequest.ShouldNotBeNull();
                secondJob.AnalysisRequest!.EnableInlineSuggestions.ShouldBeTrue();
                secondJob.AnalysisRequest.EnableSummaryComment.ShouldBeFalse();
                secondJob.AnalysisRequest.EnableDescriptionSummary.ShouldBeFalse();
                secondJob.AnalysisRequest.EnableInitialComment.ShouldBeFalse();
            }
        }
        finally
        {
            sharedClient.SourceCommitIdToReturn = originalSourceCommit;
        }
    }

    [Test]
    public async Task Handle_WhenPreviousJobCompletedForSameSourceHead_ReturnsExistingJobIdWithoutCreatingSecondJob()
    {
        var firstJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));
        await SendAsync(new UpdateAnalysisJobStatusCommand(firstJobId, AnalysisStatus.Running, StartedAt: DateTimeOffset.UtcNow));
        await SendAsync(new CompleteAnalysisJobCommand(firstJobId, "summary", "detailed", null, "TestAnalyzer"));

        var secondJobId = await SendAsync(new SubmitAnalysisCommand(TestDataBuilder.ValidRequest()));

        secondJobId.ShouldBe(firstJobId);

        var (scope, context) = GetDbContext();
        using (scope)
        {
            var jobCount = await context.AnalysisJobs.CountAsync();
            jobCount.ShouldBe(1);
        }
    }

    [Test]
    public async Task Handle_WithInvalidCommand_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataBuilder.InvalidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(SendAsync(command));
    }
}

