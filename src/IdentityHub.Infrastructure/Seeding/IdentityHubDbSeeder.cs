using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var tenantOptions = scope.ServiceProvider.GetRequiredService<IOptions<TenantConfigurationOptions>>().Value;
        var seedTenantIds = tenantOptions.SeedTenantIds
            .Where(tenantId => !string.IsNullOrWhiteSpace(tenantId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (seedTenantIds.Count == 0)
        {
            throw new InvalidOperationException($"{TenantConfigurationOptions.SectionName}:SeedTenantIds is required for startup seeding.");
        }

        // Apply pending migrations
        await db.Database.MigrateAsync();
        logger.LogInformation("Authorization database migrated");

        logger.LogInformation("Seeding authorization database for {Count} tenant(s)...", seedTenantIds.Count);

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

        foreach (var seedTenantId in seedTenantIds)
        {
            var previousContext = httpContextAccessor.HttpContext;
            httpContextAccessor.HttpContext = CreateSeederHttpContext(seedTenantId, tenantOptions.HeaderName);

            try
            {
                var hasTenantData = await db.Permissions.AnyAsync(p => p.TenantId == seedTenantId)
                    || await db.Roles.AnyAsync(r => r.TenantId == seedTenantId);

                if (hasTenantData)
                {
                    logger.LogInformation("Authorization database already seeded for tenant {TenantId} — skipping", seedTenantId);
                    continue;
                }

                logger.LogInformation("Seeding authorization database for tenant {TenantId}...", seedTenantId);

                var permissions = new Dictionary<string, Permission>();
                foreach (var permName in permissionNames)
                {
                    var permission = new Permission { Name = permName, TenantId = seedTenantId };
                    db.Permissions.Add(permission);
                    permissions[permName] = permission;
                }

                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} permissions for tenant {TenantId}", permissions.Count, seedTenantId);

                var preferredSeedRoleId = tenantOptions.SeedGroupRoleMappings
                    .Select(mapping => mapping.RoleId)
                    .FirstOrDefault();
                var seedRoleId = preferredSeedRoleId != Guid.Empty
                    && !db.ChangeTracker.Entries<Role>().Any(entry => entry.Entity.Id == preferredSeedRoleId)
                    ? preferredSeedRoleId
                    : Guid.NewGuid();

                var adminRole = new Role
                {
                    Id = seedRoleId,
                    Name = "Admin",
                    Description = "Administrator role with full system access",
                    TenantId = seedTenantId
                };
                db.Roles.Add(adminRole);
                await db.SaveChangesAsync();
                logger.LogInformation("Created Admin role for tenant {TenantId}", seedTenantId);

                foreach (var permission in permissions.Values)
                {
                    db.RolePermissions.Add(new Domain.Entities.RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                }

                await db.SaveChangesAsync();
                logger.LogInformation("Assigned all {Count} permissions to Admin role for tenant {TenantId}", permissions.Count, seedTenantId);

                foreach (var seedMapping in tenantOptions.SeedGroupRoleMappings)
                {
                    var roleIdToMap = seedMapping.RoleId == preferredSeedRoleId
                        ? adminRole.Id
                        : seedMapping.RoleId;

                    var role = await db.Roles.FirstOrDefaultAsync(
                        r => r.TenantId == seedTenantId && r.Id == roleIdToMap);

                    if (role is null)
                    {
                        logger.LogWarning(
                            "Skipping group-role mapping for tenant {TenantId} because role {RoleId} was not found",
                            seedTenantId,
                            roleIdToMap);
                        continue;
                    }

                    db.GroupRoleMappings.Add(new GroupRoleMapping
                    {
                        GroupId = seedMapping.GroupId,
                        RoleId = role.Id,
                        TenantId = seedTenantId
                    });

                    logger.LogInformation(
                        "Mapped Entra group {GroupId} to role {RoleId} for tenant {TenantId}",
                        seedMapping.GroupId,
                        roleIdToMap,
                        seedTenantId);
                }

                await db.SaveChangesAsync();
            }
            finally
            {
                httpContextAccessor.HttpContext = previousContext;
            }
        }

        foreach (var seedUserTenantMapping in tenantOptions.SeedUserTenantMappings)
        {
            if (string.IsNullOrWhiteSpace(seedUserTenantMapping.UserId)
                || string.IsNullOrWhiteSpace(seedUserTenantMapping.TenantId))
            {
                continue;
            }

            var exists = await db.UserTenantMappings.AnyAsync(m =>
                m.UserId == seedUserTenantMapping.UserId
                && m.TenantId == seedUserTenantMapping.TenantId);

            if (exists)
            {
                continue;
            }

            db.UserTenantMappings.Add(new UserTenantMapping
            {
                UserId = seedUserTenantMapping.UserId,
                TenantId = seedUserTenantMapping.TenantId
            });

            await db.SaveChangesAsync();
            logger.LogInformation(
                "Seeded user-tenant mapping for user {UserId} and tenant {TenantId}",
                seedUserTenantMapping.UserId,
                seedUserTenantMapping.TenantId);
        }

        logger.LogInformation("Authorization database seeding complete");
    }

    private static DefaultHttpContext CreateSeederHttpContext(string tenantId, string headerName)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[headerName] = tenantId;
        return context;
    }
}
