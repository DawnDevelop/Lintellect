using FluentAssertions;
using Mediator;

namespace Lintellect.Api.functionaltests.Queries;

[TestFixture]
public class GetAnalysisHistoryQueryTests : Testing
{
    [Test]
    public async Task Handle_WithNoJobs_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAnalysisHistoryQuery(0, 10);
        var mediator = await GetService<IMediator>();

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_WithJobs_ReturnsJobs()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Create a job
        var jobId = await mediator.Send(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 10);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain(job => job.Id == jobId);
    }

    [Test]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Create a job
        await mediator.Send(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 1);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_WithFilters_ReturnsFilteredJobs()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var mediator = await GetService<IMediator>();

        // Create a job
        await mediator.Send(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 10, "TestProject", "TestRepo");

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();



        result.Should().AllSatisfy(job =>
        {
            var analysisRequest = JsonSerializer.Deserialize<AnalysisRequest>(job.AnalysisRequest!);
            analysisRequest.Should().NotBeNull();

            analysisRequest.GitInfo.Should().NotBeNull();
            analysisRequest.GitInfo.ProjectName.Should().Be("TestProject");
            analysisRequest.GitInfo.RepositoryName.Should().Be("TestRepo");
        });
    }
}
