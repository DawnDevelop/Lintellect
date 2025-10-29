namespace Lintellect.Api.Application.Models;

/// <summary>
/// Base configuration options for MCP servers.
/// </summary>
public record class McpConfig(string Name, string Url, string? AuthToken = null, Dictionary<string, string>? AdditionalHeaders = null);
