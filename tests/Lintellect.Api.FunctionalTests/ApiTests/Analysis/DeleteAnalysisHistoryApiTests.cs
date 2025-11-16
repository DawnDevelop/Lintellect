using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.FunctionalTests.Utilities.Analysis;
using Lintellect.Api.FunctionalTests.Utilities.Http;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.ApiTests.Analysis;

public class DeleteAnalysisHistoryApiTests : BaseTestFixture
{
    [Test]
    public async Task DeleteAnalysisHistory_WithSpecificJobId_DeletesJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        var submitResponse = await Client.SubmitAnalysisAsync(command);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var submitResult = await submitResponse.Content.ReadAsJsonAsync<SubmitAnalysisResponse>();
        var jobId = submitResult!.JobId;

        // Act
        var response = await Client.DeleteAnalysisHistoryAsync(jobId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify job was deleted
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);
            job.ShouldBeNull();
        }
    }

    [Test]
    public async Task DeleteAnalysisHistory_WithoutJobId_DeletesAllJobs()
    {
        // Arrange
        var request1 = TestDataBuilder.ValidRequest();
        var request2 = new TestDataBuilder()
            .WithValidGitInfo()
            .WithPullRequestId(456)
            .WithRepositoryName("TestRepo2")
            .WithProjectName("TestProject")
            .WithGitProvider(EGitProvider.GitHub)
            .WithLanguage(EProgrammingLanguage.CSharp)
            .Build();

        var submitResponse1 = await Client.SubmitAnalysisAsync(new SubmitAnalysisCommand(request1));
        var submitResponse2 = await Client.SubmitAnalysisAsync(new SubmitAnalysisCommand(request2));

        submitResponse1.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitResponse2.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var jobId1 = (await submitResponse1.Content.ReadAsJsonAsync<SubmitAnalysisResponse>())!.JobId;
        var jobId2 = (await submitResponse2.Content.ReadAsJsonAsync<SubmitAnalysisResponse>())!.JobId;

        // Act
        var response = await Client.DeleteAnalysisHistoryAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify all jobs were deleted
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job1 = await context.AnalysisJobs.FindAsync(jobId1);
            var job2 = await context.AnalysisJobs.FindAsync(jobId2);

            job1.ShouldBeNull();
            job2.ShouldBeNull();
        }
    }

    [Test]
    public async Task DeleteAnalysisHistory_WithNonExistentJobId_ReturnsNoContent()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAnalysisHistoryAsync(nonExistentJobId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteAnalysisHistory_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var command = new SubmitAnalysisCommand(request);

        var submitResponse = await Client.SubmitAnalysisAsync(command);
        var jobId = (await submitResponse.Content.ReadAsJsonAsync<SubmitAnalysisResponse>())!.JobId;

        var clientWithoutKey = WebApplicationFactory.CreateClient();

        // Act
        var response = await clientWithoutKey.DeleteAnalysisHistoryAsync(jobId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

