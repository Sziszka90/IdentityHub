using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPermissionsRepository"/>.
/// </summary>
public class PermissionsRepository : IPermissionsRepository
{
    private readonly IdentityHubDbContext _db;

    public PermissionsRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default)
        => await _db.Permissions.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(ct);
        return permission;
    }

    public async Task<bool> DeletePermissionAsync(Guid id, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (perm is null)
        {
            return false;
        }

        _db.Permissions.Remove(perm);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<string>> GetPermissionsForRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        return role?.RolePermissions.Select(rp => rp.Permission.Name).ToList() ?? [];
    }

    public async Task SetRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new KeyNotFoundException($"Role '{roleId}' not found");

        _db.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var permissionId in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<Guid, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync(ct);

        return roles.ToDictionary(
            r => r.Id,
            r => r.RolePermissions.Select(rp => rp.Permission.Name).ToList());
    }
}
