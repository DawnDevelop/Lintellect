using Lintellect.Api.Application.Common.Exceptions;
using Mediator;

namespace Lintellect.Api.functionaltests.Commands;

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
        jobId.ShouldNotBe(Guid.Empty);

        // Verify job was created in database
        using var context = await GetDbContext();
        var job = await context.AnalysisJobs.FindAsync(jobId);

        job.ShouldNotBeNull();
        job!.Status.ShouldBe(AnalysisStatus.Pending);
        job.AnalysisRequest.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_WithInvalidCommand_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataBuilder.InvalidRequest();
        var command = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Act & Assert
        async Task<Guid> act()
        {
            return await mediator.Send(command);
        }

        await Should.ThrowAsync<ValidationException>((Func<Task<Guid>>)act);
    }
}
