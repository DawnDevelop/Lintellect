using System.Text.Json.Serialization;

namespace Lintellect.Api.Application.Models;

/// <summary>
/// Represents a code owner with type information for Azure DevOps integration.
/// </summary>
public class CodeOwner
{
    public string Name { get; set; } = string.Empty;
    public CodeOwnerType Type { get; set; }
    public string? Email { get; set; }
    public string? AzureDevOpsId { get; set; }
    public string? DisplayName { get; set; }
}

public class CodeOwnersResult
{
    public List<CodeOwner> CodeOwners { get; set; } = [];
}

/// <summary>
/// Enum representing the type of code owner (User or Team).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CodeOwnerType
{
    User,
    Team,
    Email
}
