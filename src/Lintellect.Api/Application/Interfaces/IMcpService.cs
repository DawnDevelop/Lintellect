using Lintellect.Api.Application.Models;
using Lintellect.Shared.Models;

namespace Lintellect.Api.Application.Interfaces;

/// <summary>
/// Interface for MCP (Model Context Protocol) services that provide additional context for code analysis.
/// </summary>
public interface IMcpService
{
    /// <summary>
    /// Gets the MCP server type this service handles.
    /// </summary>
    EMcpServer ServerType { get; }

    McpConfig GetMcpConfig();

}
