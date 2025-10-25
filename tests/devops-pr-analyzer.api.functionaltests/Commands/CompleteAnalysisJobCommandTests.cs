using Mediator;

namespace devops_pr_analyzer.api.functionaltests.Commands;

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

        job.Should().NotBeNull();
        job!.Status.Should().Be(AnalysisStatus.Completed);
        job.Summary.Should().Be("Test summary");
        job.DetailedAnalysis.Should().Be("Test detailed analysis");
        job.InlineSuggestions.Should().Be("Test inline suggestions");
        job.AnalyzerUsed.Should().Be("MockAnalyzer");
        job.CompletedAt.Should().NotBeNull();
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
        var act = async () => await mediator.Send(completeCommand);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
