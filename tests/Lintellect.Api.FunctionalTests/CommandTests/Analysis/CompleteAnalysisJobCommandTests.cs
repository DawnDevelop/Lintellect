using Lintellect.Api.Application.Messages.Commands.Analysis;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class CompleteAnalysisJobCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithValidJob_CompletesJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);

        // Create a job first
        var jobId = await SendAsync(submitCommand);

        // Start the job
        var startCommand = new UpdateAnalysisJobStatusCommand(jobId, AnalysisStatus.Running, StartedAt: DateTimeOffset.UtcNow);
        await SendAsync(startCommand);

        var completeCommand = new CompleteAnalysisJobCommand(
            jobId,
            "Test summary",
            "Test detailed analysis",
            "Test inline suggestions",
            "MockAnalyzer");

        // Act
        await SendAsync(completeCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);

            job.ShouldNotBeNull();
            job!.Status.ShouldBe(AnalysisStatus.Completed);
            job.Summary.ShouldBe("Test summary");
            job.DetailedAnalysis.ShouldBe("Test detailed analysis");
            job.InlineSuggestions.ShouldBe("Test inline suggestions");
            job.AnalyzerUsed.ShouldBe("MockAnalyzer");
            job.CompletedAt.ShouldNotBeNull();
        }
    }

    [Test]
    public async Task Handle_WithNonExistentJob_ThrowsException()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var completeCommand = new CompleteAnalysisJobCommand(
            nonExistentJobId,
            "Test summary",
            "Test detailed analysis",
            "Test inline suggestions",
            "MockAnalyzer");

        await Should.ThrowAsync<InvalidOperationException>(SendAsync(completeCommand));
    }
}

