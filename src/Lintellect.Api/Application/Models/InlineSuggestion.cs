namespace Lintellect.Api.Application.Models;

/// <summary>
/// Represents a structured inline code suggestion.
/// </summary>
public sealed record InlineSuggestion
{
    /// <summary>
    /// The file path where the suggestion applies.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The line number from where the suggestion applies.
    /// </summary>
    public required int LineFrom { get; init; }


    /// <summary>
    /// The line number to where the suggestion applies.
    /// </summary>
    public int? LineTo { get; init; }

    /// <summary>
    /// The rule ID from the analyzer (e.g., "CS1234").
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// Severity level (Error, Warning, Info).
    /// </summary>
    public string? Severity { get; init; }

    /// <summary>
    /// Brief title/description of the issue.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed explanation of why the change is needed.
    /// </summary>
    public required string Explanation { get; init; }

    /// <summary>
    /// The corrected code to suggest.
    /// </summary>
    public required string SuggestedCode { get; init; }
}

/// <summary>
/// Response structure for inline suggestions from AI.
/// </summary>
internal sealed record InlineSuggestionsResponse
{
    public List<InlineSuggestion> Suggestions { get; init; } = [];
}
