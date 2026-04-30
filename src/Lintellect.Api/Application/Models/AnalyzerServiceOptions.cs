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
    public int MaxTokens { get; set; } = 40960;

    /// <summary>
    /// Temperature for response generation (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.5;

    /// <summary>
    /// Maximum number of inline suggestions to post across the entire PR.
    /// Suggestions are selected by severity (errors first, then warnings, then info).
    /// Set to 0 to disable the cap.
    /// </summary>
    public int MaxInlineSuggestions { get; set; } = 10;

}

/// <summary>
/// Configuration options for the Azure OpenAI analyzer service (Microsoft Agent Framework).
/// </summary>
public sealed class AzureOpenAIAnalyzerOptions
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
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Maximum tokens for the response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for response generation (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; set; } = 1;

    /// <summary>
    /// Maximum number of inline suggestions to post across the entire PR.
    /// Suggestions are selected by severity (errors first, then warnings, then info).
    /// Set to 0 to disable the cap.
    /// </summary>
    public int MaxInlineSuggestions { get; set; } = 10;
}
