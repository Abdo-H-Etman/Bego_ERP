using System.Security.Claims;
using Erp.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Erp.Modules.Identity.Application;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
    public string Permission { get; } = permission;
}

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var hasPermission = await permissionService.HasPermissionAsync(userId, requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
