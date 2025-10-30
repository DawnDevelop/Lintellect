using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.MCPs;

/// <summary>
/// Microsoft Docs MCP service implementation for retrieving official Microsoft documentation.
/// </summary>
internal sealed class MicrosoftDocsMcpService() : IMcpService
{
    public EMcpServer ServerType { get; }

    public McpConfig GetMcpConfig()
    {
        return new McpConfig(
            Name: "MicrosoftDocs",
            Url: "https://learn.microsoft.com/api/mcp",
            AuthToken: null,
            AdditionalHeaders: null
        );
    }
}
