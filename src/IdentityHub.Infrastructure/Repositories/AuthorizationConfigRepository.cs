using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IAuthorizationConfigRepository for managing authorization configuration.
/// </summary>
public class AuthorizationConfigRepository : IAuthorizationConfigRepository
{
    private readonly IdentityHubDbContext _db;

    public AuthorizationConfigRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).ToListAsync(ct);
        return roles.ToDictionary(
            r => r.Name,
            r => r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
        );
    }

    public async Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default)
    {
        var mappings = await _db.GroupRoleMappings.Include(g => g.Role).ToListAsync(ct);
        return mappings.ToDictionary(m => m.GroupName, m => m.Role.Name);
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => await _db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => await _db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task AddPermissionsToRoleAsync(string roleName, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role == null || permissions.Count == 0)
            return;

        var allPermissions = await _db.Permissions.Where(p => permissions.Contains(p.Name)).ToListAsync(ct);
        var existingPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        foreach (var perm in allPermissions)
        {
            if (!existingPermissionIds.Contains(perm.Id))
            {
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemovePermissionsFromRoleAsync(string roleName, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role == null || permissions.Count == 0)
        {
            return;
        }

        var permissionIdsToRemove = await _db.Permissions
            .Where(p => permissions.Contains(p.Name))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var toRemove = role.RolePermissions.Where(rp => permissionIdsToRemove.Contains(rp.PermissionId)).ToList();
        foreach (var rp in toRemove)
        {
            role.RolePermissions.Remove(rp);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Update(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<bool> DeleteRoleAsync(int id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FindAsync([id], ct);
        if (role == null)
        {
            return false;
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetRolePermissionsAsync(string roleName, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role == null)
        {
            return;
        }

        var allPermissions = await _db.Permissions.Where(p => permissions.Contains(p.Name)).ToListAsync(ct);
        role.RolePermissions.Clear();

        foreach (var perm in allPermissions)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default)
        => await _db.GroupRoleMappings.Include(g => g.Role).AsNoTracking().ToListAsync(ct);

    public async Task<GroupRoleMapping?> GetGroupRoleMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
        => await _db.GroupRoleMappings.Include(g => g.Role).FirstOrDefaultAsync(g => g.GroupName == groupName, ct);

    public async Task<GroupRoleMapping> CreateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default)
    {
        _db.GroupRoleMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping;
    }

    public async Task<GroupRoleMapping> UpdateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default)
    {
        _db.GroupRoleMappings.Update(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping;
    }

    public async Task<bool> DeleteGroupRoleMappingAsync(int id, CancellationToken ct = default)
    {
        var mapping = await _db.GroupRoleMappings.FindAsync(new object[] { id }, ct);
        if (mapping == null) return false;
        _db.GroupRoleMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return true;
    }

}
