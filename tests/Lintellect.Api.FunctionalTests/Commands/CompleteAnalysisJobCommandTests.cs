using Mediator;

namespace Lintellect.Api.functionaltests.Commands;

[TestFixture]
public class CompleteAnalysisJobCommandTests : Testing
{
    [Test]
    public async Task Handle_WithValidJob_CompletesJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Create a job first
        var jobId = await mediator.Send(submitCommand);

        // Start the job
        var startCommand = new UpdateAnalysisJobStatusCommand(jobId, AnalysisStatus.Running);
        await mediator.Send(startCommand);

        var completeCommand = new CompleteAnalysisJobCommand(
            jobId,
            "Test summary",
            "Test detailed analysis",
            "Test inline suggestions",
            "MockAnalyzer");

        // Act
        await mediator.Send(completeCommand);

        // Assert
        using var context = await GetDbContext();
        var job = await context.AnalysisJobs.FindAsync(jobId);

        job.ShouldNotBeNull();
        job!.Status.ShouldBe(AnalysisStatus.Completed);
        job.Summary.ShouldBe("Test summary");
        job.DetailedAnalysis.ShouldBe("Test detailed analysis");
        job.InlineSuggestions.ShouldBe("Test inline suggestions");
        job.AnalyzerUsed.ShouldBe("MockAnalyzer");
        job.CompletedAt.ShouldNotBeNull();
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

        var mediator = await GetService<IMediator>();

        // Act & Assert
        async Task<Unit> act()
        {
            return await mediator.Send(completeCommand);
        }

        await Should.ThrowAsync<InvalidOperationException>((Func<Task<Unit>>)act);
    }
}
