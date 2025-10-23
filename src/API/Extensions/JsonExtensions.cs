using System.Text.Json;

namespace devops_pr_analyzer.Extensions;

public class JsonExtensions
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };
}
