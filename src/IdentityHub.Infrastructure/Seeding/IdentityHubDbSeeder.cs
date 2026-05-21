using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Infrastructure.Seeding;

/// <summary>
/// Seeds the authorization database with default permissions and Admin role.
/// Runs on startup — idempotent (only runs if database is empty).
/// </summary>
public static class AuthorizationDbSeeder
{
    public static async Task SeedFromConfigAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityHubDbContext>>();

        // Apply pending migrations
        await db.Database.MigrateAsync();
        logger.LogInformation("Authorization database migrated");

        // Skip seeding if database is not empty
        if (await db.Permissions.AnyAsync() || await db.Roles.AnyAsync())
        {
            logger.LogInformation("Authorization database already seeded — skipping");
            return;
        }

        logger.LogInformation("Seeding authorization database with default permissions and Admin role...");

        // ── 1. Define all standardized permissions ──
        var permissionNames = new List<string>
        {
            // Users Management
            "users.read",
            "users.create",
            "users.update",
            "users.delete",
            "users.permissions.read",
            "users.roles.assign",
            "users.roles.remove",

            // Roles Management
            "roles.read",
            "roles.create",
            "roles.update",
            "roles.delete",

            // Permissions Management
            "permissions.read",
            "permissions.create",
            "permissions.delete",

            // Groups Management
            "groups.read",
            "groups.create",
            "groups.update",
            "groups.delete"
        };

        // ── 2. Create all permissions ──
        var permissions = new Dictionary<string, Permission>();
        foreach (var permName in permissionNames)
        {
            var permission = new Permission { Name = permName };
            db.Permissions.Add(permission);
            permissions[permName] = permission;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions", permissions.Count);

        // ── 3. Create Admin role ──
        var adminRole = new Role
        {
            Name = "Admin",
            Description = "Administrator role with full system access"
        };
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();
        logger.LogInformation("Created Admin role");

        // ── 4. Attach all permissions to Admin role ──
        foreach (var permission in permissions.Values)
        {
            db.RolePermissions.Add(new Domain.Entities.RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Assigned all {Count} permissions to Admin role", permissions.Count);

        // ── 5. Create GroupRoleMapping for Admin group ──
        db.GroupRoleMappings.Add(new GroupRoleMapping
        {
            GroupId = new Guid("c4de85d6-0780-4280-aa9a-3a30f0a18878"),
            RoleId = adminRole.Id
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Created GroupRoleMapping: GroupName=Admin, RoleId={RoleId}", adminRole.Id);

        logger.LogInformation("Authorization database seeding complete");
    }
}
