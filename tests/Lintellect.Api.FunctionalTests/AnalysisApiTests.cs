using Lintellect.Api.Apis.Models;

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
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var result = await response.Content.ReadAsJsonAsync<SubmitAnalysisResponse>();
        result.Should().NotBeNull();
        result!.JobId.Should().NotBeEmpty();
        result.Status.Should().Be("Pending");
        result.Message.Should().Be("Analysis job submitted successfully");
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
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAnalysisStatus_WithExistingJob_ReturnsOk()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job first
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var submitResult = await submitResponse.Content.ReadAsJsonAsync<SubmitAnalysisResponse>();
        var jobId = submitResult!.JobId;

        // Act
        var response = await Client.GetAnalysisStatusAsync(jobId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<AnalysisJobStatusResponse>();
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.Status.Should().Be("Pending");
        result.ProjectName.Should().Be("TestProject");
        result.RepositoryName.Should().Be("TestRepo");
        result.PullRequestId.Should().Be(123);
    }

    [Test]
    public async Task GetAnalysisStatus_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await Client.GetAnalysisStatusAsync(nonExistentJobId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAnalysisHistory_ReturnsFilteredResults()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Act
        var response = await Client.GetAnalysisHistoryAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<IEnumerable<AnalysisJobStatusResponse>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetAnalysisHistory_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        // Submit a job
        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Act
        var response = await Client.GetAnalysisHistoryAsync(
            skip: 0,
            take: 10,
            projectName: "TestProject",
            repositoryName: "TestRepo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsJsonAsync<IEnumerable<AnalysisJobStatusResponse>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();

        // Verify filtering
        result.Should().AllSatisfy(job =>
        {
            job.ProjectName.Should().Be("TestProject");
            job.RepositoryName.Should().Be("TestRepo");
        });
    }
}
