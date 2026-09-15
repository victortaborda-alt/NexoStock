using NexoStock.Application.Auth;
using NexoStock.Infrastructure.Identity;
using NexoStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NexoStock.Infrastructure.Authorization;

public sealed class PermissionService(ApplicationDbContext dbContext) : IPermissionService
{
    public async Task<IReadOnlyCollection<RolePermissionsResponse>> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await dbContext.RolePermissions
            .AsNoTracking()
            .OrderBy(item => item.RoleName)
            .ThenBy(item => item.Permission)
            .ToListAsync(cancellationToken);

        return ApplicationRoles.All
            .OrderBy(role => role)
            .Select(role => new RolePermissionsResponse(
                role,
                permissions
                    .Where(item => string.Equals(item.RoleName, role, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Permission)
                    .ToArray()))
            .ToArray();
    }

    public async Task<RolePermissionsResponse?> UpdateRolePermissionsAsync(
        string role,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleName = ApplicationRoles.All.FirstOrDefault(item =>
            string.Equals(item, role, StringComparison.OrdinalIgnoreCase));
        if (roleName is null || request.Permissions.Any(permission => !ApplicationPermissions.All.Contains(permission)))
        {
            return null;
        }

        var existing = await dbContext.RolePermissions
            .Where(item => item.RoleName == roleName)
            .ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(existing);

        var permissions = request.Permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(permission => new RolePermission { RoleName = roleName, Permission = permission });
        await dbContext.RolePermissions.AddRangeAsync(permissions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RolePermissionsResponse(roleName, request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
