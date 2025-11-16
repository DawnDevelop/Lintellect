using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lintellect.Cli.Extensions;

internal static class JsonOptionExtensions
{
    internal static JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
