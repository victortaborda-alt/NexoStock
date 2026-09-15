using System.Security.Claims;
using NexoStock.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace NexoStock.Infrastructure.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        if (roles.Length == 0)
        {
            return;
        }

        var hasPermission = await dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(item => roles.Contains(item.RoleName) && item.Permission == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
