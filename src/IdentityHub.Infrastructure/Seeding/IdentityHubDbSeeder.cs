using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IdentityHub.Domain.Models;

namespace IdentityHub.Infrastructure.Seeding;

/// <summary>
/// Seeds the authorization database from appsettings configuration.
/// Runs on startup — idempotent (skips if data already exists).
/// </summary>
public static class AuthorizationDbSeeder
{
    public static async Task SeedFromConfigAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityHubDbContext>>();
        var rolePermOptions = scope.ServiceProvider.GetRequiredService<IOptions<RolePermissionOptions>>().Value;
        var policyOptions = scope.ServiceProvider.GetService<IOptions<AuthorizationPoliciesOptions>>()?.Value;

        // Apply pending migrations
        await db.Database.MigrateAsync();
        logger.LogInformation("Authorization database migrated");

        // Skip seeding if roles already exist
        if (await db.Roles.AnyAsync())
        {
            logger.LogInformation("Authorization database already seeded — skipping");
            return;
        }

        logger.LogInformation("Seeding authorization database from appsettings configuration...");

        // ── 1. Create Roles and Permissions from RolePermissions config ──
        var permissionCache = new Dictionary<string, Permission>();

        foreach (var (roleName, permissionNames) in rolePermOptions.RolePermissions)
        {
            var role = new Role { Name = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync(); // flush to get role Id

            foreach (var permName in permissionNames)
            {
                if (!permissionCache.TryGetValue(permName, out var perm))
                {
                    perm = await db.Permissions.FirstOrDefaultAsync(p => p.Name == permName);
                    if (perm is null)
                    {
                        perm = new Permission { Name = permName };
                        db.Permissions.Add(perm);
                        await db.SaveChangesAsync();
                    }
                    permissionCache[permName] = perm;
                }

                db.RolePermissions.Add(new Domain.Entities.RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded role {Role} with {Count} permissions", roleName, permissionNames.Count);
        }

        // ── 2. Create Group-Role mappings ──
        foreach (var (groupName, roleName) in rolePermOptions.GroupToRoleMapping)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role is null)
            {
                logger.LogWarning("Skipping group mapping {Group} → {Role}: role not found", groupName, roleName);
                continue;
            }

            db.GroupRoleMappings.Add(new GroupRoleMapping
            {
                GroupName = groupName,
                RoleId = role.Id
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} group-role mappings", rolePermOptions.GroupToRoleMapping.Count);


        logger.LogInformation("Authorization database seeding complete");
    }
}
