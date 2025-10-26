using System.Text.Json;

namespace Lintellect.Api.Infrastructure.Extensions;

public class JsonExtensions
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };
}
