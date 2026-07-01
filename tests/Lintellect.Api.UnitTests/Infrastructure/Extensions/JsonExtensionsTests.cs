using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Extensions;
using Shouldly;

namespace Lintellect.Api.UnitTests.Infrastructure.Extensions;

[TestFixture]
public class JsonExtensionsTests
{
    private const string SuggestionsJson = """
        {
          "suggestions": [
            {
              "filePath": "src/Program.cs",
              "lineFrom": 42,
              "severity": "Warning",
              "title": "Fix it",
              "explanation": "because",
              "suggestedCode": "code"
            }
          ]
        }
        """;

    [Test]
    public void DeserializeModelJson_PlainJson_ParsesWrapper()
    {
        var result = JsonExtensions.DeserializeModelJson<InlineSuggestionsResponse>(SuggestionsJson);

        result.ShouldNotBeNull();
        result.Suggestions.Count.ShouldBe(1);
        result.Suggestions[0].FilePath.ShouldBe("src/Program.cs");
    }

    [Test]
    public void DeserializeModelJson_JsonCodeFence_IsStripped()
    {
        var fenced = $"```json\n{SuggestionsJson}\n```";

        var result = JsonExtensions.DeserializeModelJson<InlineSuggestionsResponse>(fenced);

        result.ShouldNotBeNull();
        result.Suggestions.Count.ShouldBe(1);
    }

    [Test]
    public void DeserializeModelJson_BareCodeFence_IsStripped()
    {
        var fenced = $"```\n{SuggestionsJson}\n```";

        var result = JsonExtensions.DeserializeModelJson<InlineSuggestionsResponse>(fenced);

        result.ShouldNotBeNull();
        result.Suggestions.Count.ShouldBe(1);
    }

    [Test]
    public void DeserializeModelJson_CamelCaseFields_MapToRecord()
    {
        var result = JsonExtensions.DeserializeModelJson<InlineSuggestionsResponse>(SuggestionsJson);

        result!.Suggestions[0].LineFrom.ShouldBe(42);
        result.Suggestions[0].Severity.ShouldBe("Warning");
    }
}
