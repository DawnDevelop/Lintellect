namespace devops_pr_analyzer.Application.Models;

public record CheckPermissionResult(
    bool HasPermission,
    string? Reason = null);
