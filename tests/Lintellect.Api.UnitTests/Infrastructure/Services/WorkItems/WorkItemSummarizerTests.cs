using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Infrastructure.Services.WorkItems;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Lintellect.Api.UnitTests.Infrastructure.Services.WorkItems;

[TestFixture]
public class WorkItemSummarizerTests
{
    [Test]
    public void SplitGoalAndContext_ExtractsBothSections()
    {
        var response = """
            GOAL: Migrate auth middleware to comply with the new session token policy.

            CONTEXT:
            The legacy middleware stores raw session tokens in the response cache.
            Compliance requires hashed tokens with a 30-minute TTL.
            """;

        var (goal, context) = WorkItemSummarizer.SplitGoalAndContext(response);

        goal.ShouldBe("Migrate auth middleware to comply with the new session token policy.");
        context.ShouldContain("Compliance requires hashed tokens");
    }

    [Test]
    public void SplitGoalAndContext_WithoutContextSection_ReturnsEmptyContext()
    {
        var response = "GOAL: Tighten the rate limiter.";

        var (goal, context) = WorkItemSummarizer.SplitGoalAndContext(response);

        goal.ShouldBe("Tighten the rate limiter.");
        context.ShouldBeEmpty();
    }

    [Test]
    public async Task SummarizeAsync_WithEmptyList_ReturnsEmpty()
    {
        var analyzer = Substitute.For<IAnalyzerService>();
        var summarizer = new WorkItemSummarizer(analyzer, NullLogger<WorkItemSummarizer>.Instance);

        var result = await summarizer.SummarizeAsync([]);

        result.ShouldBe(WorkItemSummary.Empty);
        await analyzer.DidNotReceive().SummarizeContextAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SummarizeAsync_PassesItemDelimitersInPrompt()
    {
        var analyzer = Substitute.For<IAnalyzerService>();
        analyzer.SummarizeContextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("GOAL: Do X.\n\nCONTEXT:\nBecause Y.");

        var summarizer = new WorkItemSummarizer(analyzer, NullLogger<WorkItemSummarizer>.Instance);
        var items = new List<WorkItemReference>
        {
            new("11", Title: "First", Body: "First body"),
            new("22", Title: "Second", Body: "Second body")
        };

        var result = await summarizer.SummarizeAsync(items);

        result.Goal.ShouldBe("Do X.");
        result.FullContext.ShouldContain("GOAL:");

        await analyzer.Received(1).SummarizeContextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p => p.Contains("Work Item 1 of 2") && p.Contains("Work Item 2 of 2")),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SummarizeAsync_WithEmptyResponse_ReturnsEmpty()
    {
        var analyzer = Substitute.For<IAnalyzerService>();
        analyzer.SummarizeContextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var summarizer = new WorkItemSummarizer(analyzer, NullLogger<WorkItemSummarizer>.Instance);

        var result = await summarizer.SummarizeAsync([new WorkItemReference("1", Title: "T")]);

        result.ShouldBe(WorkItemSummary.Empty);
    }
}
