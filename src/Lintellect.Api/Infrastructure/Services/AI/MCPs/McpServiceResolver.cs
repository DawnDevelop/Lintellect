using Lintellect.Api.Application.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Infrastructure.Services.AI.MCPs;

/// <summary>
/// MCP service resolver implementation that provides access to configured MCP services.
/// </summary>
internal sealed class McpServiceResolver(IServiceProvider serviceProvider) : IMcpServiceResolver
{
    public IMcpService? GetMcpService(EMcpServer serverType)
    {
        if (serverType == EMcpServer.None)
        {
            return null;
        }

        var service = serviceProvider.GetKeyedService<IMcpService>(serverType);
        return service;
    }
}
