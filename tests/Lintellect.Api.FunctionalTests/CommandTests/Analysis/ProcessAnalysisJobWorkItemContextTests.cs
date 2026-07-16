using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.FunctionalTests.Mocks.AI;
using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests.CommandTests.Analysis;

public class ProcessAnalysisJobWorkItemContextTests : BaseTestFixture
{
    [Test]
    public async Task Handle_WhenWorkItemContextDisabled_FeedsNoContextIntoPrompts()
    {
        var request = TestDataBuilder.ValidRequest();
        request.EnableWorkItemContext = false;

        var mockAnalyzer = await GetMockAnalyzerAsync();

        await SendAsync(new ProcessAnalysisJobCommand(Guid.NewGuid(), request));

        mockAnalyzer.LastSummaryWorkItemContext.ShouldBeEmpty();
        mockAnalyzer.LastDetailedWorkItemContext.ShouldBeEmpty();
        mockAnalyzer.LastInlineWorkItemGoal.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenWorkItemContextEnabled_FeedsWorkItemsIntoPrompts()
    {
        var request = TestDataBuilder.ValidRequest();
        request.EnableWorkItemContext = true;

        var mockAnalyzer = await GetMockAnalyzerAsync();

        await SendAsync(new ProcessAnalysisJobCommand(Guid.NewGuid(), request));

        mockAnalyzer.LastSummaryWorkItemContext.ShouldNotBeNull().ShouldContain("## Linked Work Items");
        mockAnalyzer.LastSummaryWorkItemContext.ShouldContain("100: Add foo support");
        mockAnalyzer.LastSummaryWorkItemContext.ShouldContain("Implement foo per spec.");
        mockAnalyzer.LastDetailedWorkItemContext.ShouldNotBeNull().ShouldContain("## Linked Work Items");
        mockAnalyzer.LastInlineWorkItemGoal.ShouldBe(
            "The intent of this PR (from its linked work items): Add foo support");
    }

    private static async Task<MockAnalyzerService> GetMockAnalyzerAsync()
    {
        var analyzer = await GetService<IAnalyzerService>();
        return analyzer as MockAnalyzerService
            ?? throw new InvalidOperationException("Test fixture must register MockAnalyzerService.");
    }
}
