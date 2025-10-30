using Lintellect.Api.Application.Messages.Commands.Analysis;
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
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
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
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain(job => job.Id == jobId);
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
        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem();
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
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();

        foreach (var job in result)
        {
            var analysisRequest = JsonSerializer.Deserialize<AnalysisRequest>(job.AnalysisRequest!);
            analysisRequest.ShouldNotBeNull();

            analysisRequest.GitInfo.ShouldNotBeNull();
            analysisRequest.GitInfo.ProjectName.ShouldBe("TestProject");
            analysisRequest.GitInfo.RepositoryName.ShouldBe("TestRepo");
        }
    }
}
