using Lintellect.Api.Application.Services;
using Lintellect.Shared.Models;

namespace Lintellect.Api.UnitTests.Application.Services;

[TestFixture]
public class WorkItemPromptFormatterTests
{
    private static WorkItemReference Item(string id = "42", string? title = "Add foo", string? body = "Do the thing.") =>
        new(id, Title: title, Body: body, Type: "User Story", State: "Active");

    [Test]
    public void ToPromptBlock_NoItems_ReturnsEmpty()
    {
        WorkItemPromptFormatter.ToPromptBlock([]).ShouldBeEmpty();
    }

    [Test]
    public void ToPromptBlock_FormatsItemWithHeadingAndDirective()
    {
        var block = WorkItemPromptFormatter.ToPromptBlock([Item()]);

        block.ShouldContain("## Linked Work Items");
        block.ShouldContain("### 42: Add foo");
        block.ShouldContain("Type: User Story");
        block.ShouldContain("State: Active");
        block.ShouldContain("Do the thing.");
        block.ShouldContain("Evaluate whether the changes fulfill");
    }

    [Test]
    public void ToPromptBlock_TruncatesLongBodies()
    {
        var block = WorkItemPromptFormatter.ToPromptBlock([Item(body: new string('x', 5000))]);

        block.ShouldContain("... (truncated)");
        block.Length.ShouldBeLessThan(5000);
    }

    [Test]
    public void ToGoalPromptLine_JoinsTitles()
    {
        var line = WorkItemPromptFormatter.ToGoalPromptLine([Item(title: "Add foo"), Item("43", title: "Fix bar")]);

        line.ShouldBe("The intent of this PR (from its linked work items): Add foo; Fix bar");
    }

    [Test]
    public void ToGoalPromptLine_NoTitles_ReturnsEmpty()
    {
        WorkItemPromptFormatter.ToGoalPromptLine([Item(title: null)]).ShouldBeEmpty();
        WorkItemPromptFormatter.ToGoalPromptLine([]).ShouldBeEmpty();
    }
}
