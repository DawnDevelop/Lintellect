namespace Lintellect.Api.Application.Models.Git;

/// <summary>
/// Represents a user or identity from any Git provider.
/// This is a provider-agnostic model following Clean Architecture principles.
/// </summary>
public sealed class IdentityRef
{
    /// <summary>
    /// The display name of the user.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// The unique name/identifier of the user (e.g., username, email).
    /// </summary>
    public string? UniqueName { get; init; }

    /// <summary>
    /// The unique identifier of the user (provider-specific ID).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// The URL to the user's profile.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The URL to the user's avatar/image.
    /// </summary>
    public string? ImageUrl { get; init; }
}

