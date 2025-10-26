using System.Text.Json;

namespace Lintellect.Cli.Extensions;

internal static class JsonOptionExtensions
{
    internal static JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}
