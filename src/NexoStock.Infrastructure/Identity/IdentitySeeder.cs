using NexoStock.Application.Auth;
using NexoStock.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexoStock.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { ApplicationRoles.Administrator, ApplicationRoles.Warehouse })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var defaultPermissions = new Dictionary<string, IReadOnlyCollection<string>>
        {
            [ApplicationRoles.Administrator] = ApplicationPermissions.All,
            [ApplicationRoles.Warehouse] = new[] { ApplicationPermissions.ProfileRead }
        };
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        foreach (var permission in defaultPermissions.SelectMany(item => item.Value.Select(value => new RolePermission
                 {
                     RoleName = item.Key,
                     Permission = value
                 })))
        {
            var exists = await dbContext.RolePermissions.AnyAsync(item =>
                item.RoleName == permission.RoleName && item.Permission == permission.Permission);
            if (!exists)
            {
                dbContext.RolePermissions.Add(permission);
            }
        }

        await dbContext.SaveChangesAsync();

        var adminUserName = configuration["SeedAdmin:UserName"];
        var adminPassword = configuration["SeedAdmin:Password"];
        var adminEmail = configuration["SeedAdmin:Email"];

        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var existingUser = await userManager.FindByNameAsync(adminUserName);
        if (existingUser is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            FullName = "Administrador inicial",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoles.Administrator);
        }
    }
}
