using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Interface for resolving MCP services based on server type.
/// </summary>
public interface IMcpServiceResolver
{
    /// <summary>
    /// Gets the MCP service for the specified server type.
    /// </summary>
    /// <param name="serverType">The MCP server type.</param>
    /// <returns>The MCP service instance.</returns>
    IMcpService? GetMcpService(EMcpServer serverType);

    /// <summary>
    /// Gets all available MCP services.
    /// </summary>
    /// <returns>Collection of available MCP services.</returns>
    IEnumerable<IMcpService> GetAvailableMcpServices();
}
