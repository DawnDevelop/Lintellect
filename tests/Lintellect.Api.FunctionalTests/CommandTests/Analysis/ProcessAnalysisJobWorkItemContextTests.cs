using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.FunctionalTests.Mocks.AI;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class ProcessAnalysisJobWorkItemContextTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WhenWorkItemContextDisabled_DoesNotCallSummarizer()
    {
        var request = TestDataBuilder.ValidRequest();
        request.EnableWorkItemContext = false;

        var mockAnalyzer = await GetMockAnalyzerAsync();

        await SendAsync(new ProcessAnalysisJobCommand(Guid.NewGuid(), request));

        mockAnalyzer.SummarizeContextCallCount.ShouldBe(0);
        mockAnalyzer.LastSummaryWorkItemContext.ShouldBeEmpty();
        mockAnalyzer.LastDetailedWorkItemContext.ShouldBeEmpty();
        mockAnalyzer.LastInlineWorkItemGoal.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenWorkItemContextEnabled_FeedsContextIntoPrompts()
    {
        var request = TestDataBuilder.ValidRequest();
        request.EnableWorkItemContext = true;

        var mockAnalyzer = await GetMockAnalyzerAsync();

        await SendAsync(new ProcessAnalysisJobCommand(Guid.NewGuid(), request));

        mockAnalyzer.SummarizeContextCallCount.ShouldBe(1);
        mockAnalyzer.LastSummaryWorkItemContext.ShouldNotBeNull().ShouldContain("GOAL:");
        mockAnalyzer.LastDetailedWorkItemContext.ShouldNotBeNull().ShouldContain("GOAL:");
        mockAnalyzer.LastInlineWorkItemGoal.ShouldBe("Implement the linked work item.");
    }

    private static async Task<MockAnalyzerService> GetMockAnalyzerAsync()
    {
        var analyzer = await GetService<IAnalyzerService>();
        return analyzer as MockAnalyzerService
            ?? throw new InvalidOperationException("Test fixture must register MockAnalyzerService.");
    }
}
