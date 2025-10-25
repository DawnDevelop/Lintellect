namespace devops_pr_analyzer.Domain.Common;

/// <summary>
/// Base entity with audit fields following CleanArchitecture pattern.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
