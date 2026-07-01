using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lintellect.Api.Infrastructure.Extensions;

public class JsonExtensions
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Deserializes model output that may be wrapped in a Markdown code fence
    /// (e.g. ```json ... ```), using the shared serializer options.
    /// </summary>
    public static T? DeserializeModelJson<T>(string raw)
    {
        return JsonSerializer.Deserialize<T>(StripCodeFence(raw), JsonSerializerOptions);
    }

    private static string StripCodeFence(string raw)
    {
        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
        {
            return trimmed;
        }

        var body = trimmed[(firstNewline + 1)..];
        if (body.EndsWith("```", StringComparison.Ordinal))
        {
            body = body[..^3];
        }

        return body.Trim();
    }
}
