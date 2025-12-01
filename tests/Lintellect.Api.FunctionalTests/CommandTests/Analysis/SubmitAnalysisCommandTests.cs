using Lintellect.Api.Application.Common.Exceptions;
using Lintellect.Api.Application.Messages.Commands.Analysis;
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
    public async Task Handle_WithInvalidCommand_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataBuilder.InvalidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(SendAsync(command));
    }
}

