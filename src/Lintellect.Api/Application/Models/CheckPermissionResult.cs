namespace Lintellect.Api.Application.Models;

public record CheckPermissionResult(
    bool HasPermission,
    string? Reason = null);
