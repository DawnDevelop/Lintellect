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
}
