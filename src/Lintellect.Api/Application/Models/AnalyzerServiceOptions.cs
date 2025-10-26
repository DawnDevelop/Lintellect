using Azure.Core;

namespace Lintellect.Api.Application.Models;

/// <summary>
/// Configuration options for the Claude analyzer service.
/// </summary>
public sealed class ClaudeAnalyzerOptions
{
    /// <summary>
    /// The API key for authenticating with Claude API.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The model version to use (e.g., "claude-sonnet-4-5-20250929").
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";

    /// <summary>
    /// Maximum tokens for the response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for response generation (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Configuration options for the Semantic Kernel (AIFoundry) analyzer service.
/// </summary>
public sealed class SemanticAnalyzerOptions
{
    /// <summary>
    /// The API key for authenticating with the AI service.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The endpoint URL for the AI service.
    /// </summary>
    public string? Endpoint { get; set; }

    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// The deployment name or model to use.
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum tokens for the response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for response generation (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}
