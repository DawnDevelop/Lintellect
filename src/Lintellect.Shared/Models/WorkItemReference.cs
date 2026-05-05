namespace Lintellect.Shared.Models;

/// <summary>
/// Reference to a work item / issue linked to a pull request.
/// Carries enough information for the AI to use it as PR context.
/// </summary>
public sealed record WorkItemReference(
    string Id,
    string? Url = null,
    string? Title = null,
    string? Body = null,
    string? State = null,
    string? Type = null);
