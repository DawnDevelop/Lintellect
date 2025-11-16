using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.FunctionalTests.Utilities.Analysis;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class DeleteAnalysisHistoryCommandTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WithSpecificJobId_DeletesJob()
    {
        // Arrange
        var request = TestDataBuilder.ValidRequest();
        var submitCommand = new SubmitAnalysisCommand(request);
        var jobId = await SendAsync(submitCommand);

        var deleteCommand = new DeleteAnalysisHistoryCommand(jobId);

        // Act
        await SendAsync(deleteCommand);

        // Assert
        var (scope, context) = GetDbContext();
        using (scope)
        {
            var job = await context.AnalysisJobs.FindAsync(jobId);
            job.ShouldBeNull();
        }
    }

    [Test]
    public async Task Handle_WithEmptyJobId_DeletesAllJobs()
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

        var jobId1 = await SendAsync(new SubmitAnalysisCommand(request1));
        var jobId2 = await SendAsync(new SubmitAnalysisCommand(request2));

        var deleteCommand = new DeleteAnalysisHistoryCommand(Guid.Empty);

        // Act
        await SendAsync(deleteCommand);

        // Assert
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
    public async Task Handle_WithNonExistentJobId_CompletesWithoutError()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var deleteCommand = new DeleteAnalysisHistoryCommand(nonExistentJobId);

        // Act
        await SendAsync(deleteCommand);

        // Assert - Command should complete without throwing
    }
}

