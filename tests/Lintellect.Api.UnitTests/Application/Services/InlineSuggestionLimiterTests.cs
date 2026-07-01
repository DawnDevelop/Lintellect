using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Services;
using Shouldly;

namespace Lintellect.Api.UnitTests.Application.Services;

[TestFixture]
public class InlineSuggestionLimiterTests
{
    private static InlineSuggestion Suggestion(string severity) => new()
    {
        FilePath = "src/Program.cs",
        LineFrom = 1,
        Title = "title",
        Explanation = "why",
        SuggestedCode = "code",
        Severity = severity
    };

    [Test]
    public void ApplyGlobalCap_UnderLimit_ReturnsAll()
    {
        var suggestions = new List<InlineSuggestion> { Suggestion("info"), Suggestion("warning") };

        var result = InlineSuggestionLimiter.ApplyGlobalCap(suggestions, 10);

        result.Count.ShouldBe(2);
    }

    [Test]
    public void ApplyGlobalCap_OverLimit_KeepsHighestSeverityFirst()
    {
        var suggestions = new List<InlineSuggestion>
        {
            Suggestion("info"),
            Suggestion("error"),
            Suggestion("warning")
        };

        var result = InlineSuggestionLimiter.ApplyGlobalCap(suggestions, 2);

        result.Count.ShouldBe(2);
        result.Select(s => s.Severity).ShouldBe(["error", "warning"]);
    }

    [Test]
    public void ApplyGlobalCap_MaxZeroOrLess_ReturnsAll()
    {
        var suggestions = new List<InlineSuggestion> { Suggestion("error"), Suggestion("info") };

        var result = InlineSuggestionLimiter.ApplyGlobalCap(suggestions, 0);

        result.Count.ShouldBe(2);
    }

    [Test]
    public void ComputeMaxSuggestionsPerFile_NoFiles_ReturnsDefault()
    {
        InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(0, 10).ShouldBe(5);
    }

    [Test]
    public void ComputeMaxSuggestionsPerFile_SmallPr_CappedAtFive()
    {
        InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(1, 10).ShouldBe(5);
    }

    [Test]
    public void ComputeMaxSuggestionsPerFile_LargePr_TightensBudget()
    {
        InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(5, 10).ShouldBe(2);
    }

    [Test]
    public void ComputeMaxSuggestionsPerFile_VeryLargePr_NeverBelowOne()
    {
        InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(100, 10).ShouldBe(1);
    }

    [Test]
    public void ComputeMaxSuggestionsPerFile_GlobalCapDisabled_ReturnsDefault()
    {
        InlineSuggestionLimiter.ComputeMaxSuggestionsPerFile(3, 0).ShouldBe(5);
    }
}
