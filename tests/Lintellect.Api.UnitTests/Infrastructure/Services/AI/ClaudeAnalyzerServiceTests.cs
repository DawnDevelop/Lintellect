using Lintellect.Api.Infrastructure.Services.AI;

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

    [Test]
    public void InlineSuggestionsOutputFormat_ExposesSuggestionsWrapperSchema()
    {
        var format = ClaudeAnalyzerService.InlineSuggestionsOutputFormat;

        format.Type.ShouldBe("json_schema");
        var schema = format.Schema.GetRawText();
        schema.ShouldContain("\"suggestions\"");
        schema.ShouldContain("\"filePath\"");
        schema.ShouldContain("\"lineFrom\"");
    }

    [Test]
    public void CodeOwnersOutputFormat_ExposesCodeOwnersSchema()
    {
        var format = ClaudeAnalyzerService.CodeOwnersOutputFormat;

        format.Type.ShouldBe("json_schema");
        format.Schema.GetRawText().ShouldContain("\"codeOwners\"");
    }
}
