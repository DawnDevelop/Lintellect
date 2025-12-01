using Lintellect.Api.Application.Messages.Commands.Analysis;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.QueryTests;

public class GetAnalysisStatusQueryTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithExistingJob_ReturnsJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);

        // Create a job first
        var jobId = await SendAsync(submitCommand);

        var query = new GetAnalysisStatusQuery(jobId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(jobId);
        result.Status.ShouldBe(AnalysisStatus.Pending);
        result.AnalysisRequest.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_WithNonExistentJob_ReturnsNull()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var query = new GetAnalysisStatusQuery(nonExistentJobId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeNull();
    }
}
