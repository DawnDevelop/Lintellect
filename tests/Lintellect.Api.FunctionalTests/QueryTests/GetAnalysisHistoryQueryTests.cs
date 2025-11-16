using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Application.Messages.Queries;
using Lintellect.Api.FunctionalTests.Utilities.Analysis;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.QueryTests;

public class GetAnalysisHistoryQueryTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithNoJobs_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAnalysisHistoryQuery(0, 10);

        // Act
        var result = await SendAsync(query);

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

        // Create a job
        var jobId = await SendAsync(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 10);

        // Act
        var result = await SendAsync(query);

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

        // Create a job
        await SendAsync(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 1);

        // Act
        var result = await SendAsync(query);

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

        // Create a job
        await SendAsync(submitCommand);

        var query = new GetAnalysisHistoryQuery(0, 10, "TestProject", "TestRepo");

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();

        foreach (var job in result)
        {
            var analysisRequest = job.AnalysisRequest;
            analysisRequest.ShouldNotBeNull();

            analysisRequest.GitInfo.ShouldNotBeNull();
            analysisRequest.GitInfo.ProjectName.ShouldBe("TestProject");
            analysisRequest.GitInfo.RepositoryName.ShouldBe("TestRepo");
        }
    }
}
