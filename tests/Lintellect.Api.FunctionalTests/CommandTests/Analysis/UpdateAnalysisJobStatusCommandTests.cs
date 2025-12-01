using Lintellect.Api.Application.Messages.Commands.Analysis;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class UpdateAnalysisJobStatusCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithPendingJob_UpdatesToRunning()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var jobId = await SendAsync(submitCommand);

        var updateCommand = new UpdateAnalysisJobStatusCommand(
            jobId,
            AnalysisStatus.Running,
            StartedAt: DateTimeOffset.UtcNow);

        // Act
        await SendAsync(updateCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);

            job.ShouldNotBeNull();
            job!.Status.ShouldBe(AnalysisStatus.Running);
            job.StartedAt.ShouldNotBeNull();
        }
    }

    [Test]
    public async Task Handle_WithRunningJob_UpdatesToFailed()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var jobId = await SendAsync(submitCommand);

        // Start the job first
        var startCommand = new UpdateAnalysisJobStatusCommand(jobId, AnalysisStatus.Running, StartedAt: DateTimeOffset.UtcNow);
        await SendAsync(startCommand);

        var failCommand = new UpdateAnalysisJobStatusCommand(
            jobId,
            AnalysisStatus.Failed,
            ErrorMessage: "Test error message");

        // Act
        await SendAsync(failCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);

            job.ShouldNotBeNull();
            job!.Status.ShouldBe(AnalysisStatus.Failed);
            job.ErrorMessage.ShouldBe("Test error message");
            job.CompletedAt.ShouldNotBeNull();
        }
    }

    [Test]
    public async Task Handle_WithNonExistentJob_CompletesWithoutError()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var updateCommand = new UpdateAnalysisJobStatusCommand(
            nonExistentJobId,
            AnalysisStatus.Running,
            StartedAt: DateTimeOffset.UtcNow);

        // Act
        await SendAsync(updateCommand);

        // Assert - Command should complete without throwing (handler returns default if job not found)
    }

    [Test]
    public async Task Handle_WithNonPendingJob_DoesNotUpdateToRunning()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var jobId = await SendAsync(submitCommand);

        // Complete the job first
        var startCommand = new UpdateAnalysisJobStatusCommand(jobId, AnalysisStatus.Running, StartedAt: DateTimeOffset.UtcNow);
        await SendAsync(startCommand);

        var completeCommand = new CompleteAnalysisJobCommand(
            jobId,
            "Summary",
            "Analysis",
            null,
            "Analyzer");
        await SendAsync(completeCommand);

        // Try to update back to running (should not work because job is already Completed)
        var updateCommand = new UpdateAnalysisJobStatusCommand(
            jobId,
            AnalysisStatus.Running,
            StartedAt: DateTimeOffset.UtcNow);

        // Act
        await SendAsync(updateCommand);

        // Assert - Status should remain Completed
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);

            job.ShouldNotBeNull();
            job!.Status.ShouldBe(AnalysisStatus.Completed);
        }
    }
}

