using System.Text.Json;

namespace devops_pr_analyzer.Infrastructure.Extensions;

public class JsonExtensions
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };
}
