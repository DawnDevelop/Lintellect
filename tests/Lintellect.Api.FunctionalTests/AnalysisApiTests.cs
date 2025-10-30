using Lintellect.Api.Application.Messages.Commands.Analysis;

namespace Lintellect.Api.functionaltests;

[TestFixture]
public class AnalysisApiTests : Testing
{
    [Test]
    public async Task SubmitAnalysis_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Act
        var response = await Client.SubmitAnalysisAsync(command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var result = await response.Content.ReadAsJsonAsync<SubmitAnalysisResponse>();
        result.ShouldNotBeNull();
        result!.JobId.ShouldNotBe(Guid.Empty);
        result.Status.ShouldBe("Pending");
        result.Message.ShouldBe("Analysis job submitted successfully");
    }

    [Test]
    public async Task SubmitAnalysis_WithInvalidGitInfo_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.InvalidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Act
        var response = await Client.SubmitAnalysisAsync(command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAnalysisStatus_WithExistingJob_ReturnsOk()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job first
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var submitResult = await submitResponse.Content.ReadAsJsonAsync<SubmitAnalysisResponse>();
        var jobId = submitResult!.JobId;

        // Act
        var response = await Client.GetAnalysisStatusAsync(jobId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<AnalysisJobStatusResponse>();
        result.ShouldNotBeNull();
        result!.JobId.ShouldBe(jobId);
        result.Status.ShouldBe("Pending");
        result.ProjectName.ShouldBe("TestProject");
        result.RepositoryName.ShouldBe("TestRepo");
        result.PullRequestId.ShouldBe(123);
    }

    [Test]
    public async Task GetAnalysisStatus_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await Client.GetAnalysisStatusAsync(nonExistentJobId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAnalysisHistory_ReturnsFilteredResults()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        // Act
        var response = await Client.GetAnalysisHistoryAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<IEnumerable<AnalysisJobStatusResponse>>();
        result.ShouldNotBeNull();
        result!.ShouldNotBeEmpty();
    }

    [Test]
    public async Task GetAnalysisHistory_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        // Act
        var response = await Client.GetAnalysisHistoryAsync(
            skip: 0,
            take: 10,
            projectName: "TestProject",
            repositoryName: "TestRepo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<IEnumerable<AnalysisJobStatusResponse>>();
        result.ShouldNotBeNull();
        result!.ShouldNotBeEmpty();

        // Verify filtering
        foreach (var job in result)
        {
            job.ProjectName.ShouldBe("TestProject");
            job.RepositoryName.ShouldBe("TestRepo");
        }
    }
}
