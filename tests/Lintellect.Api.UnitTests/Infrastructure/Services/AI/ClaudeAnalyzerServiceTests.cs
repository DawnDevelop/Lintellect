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
        var schema = format.Schema;
        schema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        var suggestionItem = schema.GetProperty("properties").GetProperty("suggestions").GetProperty("items");
        suggestionItem.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        suggestionItem.GetProperty("properties").TryGetProperty("filePath", out _).ShouldBeTrue();
        suggestionItem.GetProperty("properties").TryGetProperty("lineFrom", out _).ShouldBeTrue();
    }

    [Test]
    public void CodeOwnersOutputFormat_ExposesCodeOwnersSchema()
    {
        var format = ClaudeAnalyzerService.CodeOwnersOutputFormat;

        format.Type.ShouldBe("json_schema");
        format.Schema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        format.Schema.GetProperty("properties").TryGetProperty("codeOwners", out _).ShouldBeTrue();
    }
}
