using devops_pr_analyzer.Application.Common.Exceptions;
using Mediator;

namespace devops_pr_analyzer.api.functionaltests.Commands;

[TestFixture]
public class SubmitAnalysisCommandTests : Testing
{
    [Test]
    public async Task Handle_WithValidCommand_CreatesJobAndEnqueues()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Act
        var jobId = await mediator.Send(command);

        // Assert
        jobId.Should().NotBeEmpty();

        // Verify job was created in database
        using var context = await GetDbContext();
        var job = await context.AnalysisJobs.FindAsync(jobId);

        job.Should().NotBeNull();
        job!.Status.Should().Be(AnalysisStatus.Pending);
        job.AnalysisRequest.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_WithInvalidCommand_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataBuilder.InvalidRequest();
        var command = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Act & Assert
        var act = async () => await mediator.Send(command);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
