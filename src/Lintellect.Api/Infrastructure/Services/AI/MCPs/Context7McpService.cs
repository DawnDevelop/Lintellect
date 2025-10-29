using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.MCPs;

/// <summary>
/// Context7 MCP service implementation for retrieving documentation and code examples.
/// </summary>
internal sealed class Context7McpService() : IMcpService
{

    public EMcpServer ServerType => EMcpServer.Context7;

    public McpConfig GetMcpConfig()
    {
        var apiKey = Environment.GetEnvironmentVariable("CONTEXT7_API_KEY") ?? string.Empty;
        var header = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            header.Add("CONTEXT7_API_KEY", apiKey);
        }

        return new McpConfig(
            Name: "Context7",
            Url: "https://mcp.context7.com/mcp",
            AuthToken: apiKey,
            AdditionalHeaders: header
        );
    }
}
