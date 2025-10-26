using Lintellect.Api.Domain.Enums;
using Mediator;

namespace Lintellect.Api.functionaltests.Queries;

[TestFixture]
public class GetAnalysisStatusQueryTests : Testing
{
    [Test]
    public async Task Handle_WithExistingJob_ReturnsJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Create a job first
        var jobId = await mediator.Send(submitCommand);

        var query = new GetAnalysisStatusQuery(jobId);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(jobId);
        result.Status.Should().Be(AnalysisStatus.Pending);
        result.AnalysisRequest.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_WithNonExistentJob_ReturnsNull()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var query = new GetAnalysisStatusQuery(nonExistentJobId);
        var mediator = await GetService<IMediator>();

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }
}
