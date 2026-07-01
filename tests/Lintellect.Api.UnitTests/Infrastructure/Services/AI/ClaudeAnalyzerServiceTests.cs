using Lintellect.Api.Infrastructure.Services.AI;
using Shouldly;

namespace Lintellect.Api.UnitTests.Infrastructure.Services.AI;

[TestFixture]
public class ClaudeAnalyzerServiceParseInlineSuggestionsTests
{
    private const string OneSuggestion = """
        "filePath": "src/App.cs", "lineFrom": 1, "title": "t", "explanation": "e", "suggestedCode": "c"
        """;

    [Test]
    public void ParseInlineSuggestions_WrapperObject_ReturnsSuggestions()
    {
        var raw = $$"""{ "suggestions": [ { {{OneSuggestion}} } ] }""";

        var result = ClaudeAnalyzerService.ParseInlineSuggestions(raw);

        result.Count.ShouldBe(1);
        result[0].FilePath.ShouldBe("src/App.cs");
    }

    [Test]
    public void ParseInlineSuggestions_BareArray_FallsBack()
    {
        var raw = $$"""[ { {{OneSuggestion}} } ]""";

        var result = ClaudeAnalyzerService.ParseInlineSuggestions(raw);

        result.Count.ShouldBe(1);
        result[0].FilePath.ShouldBe("src/App.cs");
    }

    [Test]
    public void ParseInlineSuggestions_FencedBareArray_FallsBack()
    {
        var raw = $"```json\n[ {{ {OneSuggestion} }} ]\n```";

        var result = ClaudeAnalyzerService.ParseInlineSuggestions(raw);

        result.Count.ShouldBe(1);
    }

    [Test]
    public void ParseInlineSuggestions_EmptyWrapper_ReturnsEmptyWithoutFallback()
    {
        var result = ClaudeAnalyzerService.ParseInlineSuggestions("""{ "suggestions": [] }""");

        result.ShouldBeEmpty();
    }
}
