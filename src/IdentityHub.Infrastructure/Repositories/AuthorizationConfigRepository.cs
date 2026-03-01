using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuthorizationConfigRepository"/>.
/// </summary>
public class AuthorizationConfigRepository : IAuthorizationConfigRepository
{
    private readonly IdentityHubDbContext _db;

    public AuthorizationConfigRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    // ────────────────────────── Roles ──────────────────────────

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        role.UpdatedAt = DateTime.UtcNow;
        _db.Roles.Update(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<bool> DeleteRoleAsync(int id, CancellationToken ct = default)
    {
        var role = await _db.Roles.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (role is null)
        {
            return false;
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ────────────────────────── Permissions ──────────────────────────

    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<Permission?> GetPermissionByIdAsync(int id, CancellationToken ct = default)
        => await _db.Permissions.Where(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default)
        => await _db.Permissions.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(ct);
        return permission;
    }

    public async Task<bool> DeletePermissionAsync(int id, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (perm is null)
        {
            return false;
        }

        _db.Permissions.Remove(perm);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ────────────────────────── Role ↔ Permission ──────────────────────────

    public async Task<List<string>> GetPermissionsForRoleAsync(string roleName, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName, ct);

        return role?.RolePermissions.Select(rp => rp.Permission.Name).ToList() ?? [];
    }

    public async Task SetRolePermissionsAsync(string roleName, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw new KeyNotFoundException($"Role '{roleName}' not found");

        _db.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var permName in permissions)
        {
            var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Name == permName, ct);
            if (perm is null)
            {
                perm = new Permission { Name = permName };
                _db.Permissions.Add(perm);
                await _db.SaveChangesAsync(ct);
            }

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = perm.Id
            });
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync(ct);

        return roles.ToDictionary(
            r => r.Name,
            r => r.RolePermissions.Select(rp => rp.Permission.Name).ToList());
    }

    // ────────────────────────── Group-Role mappings ──────────────────────────

    public async Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default)
        => await _db.GroupRoleMappings
            .Include(g => g.Role)
            .AsNoTracking()
            .OrderBy(g => g.GroupName)
            .ToListAsync(ct);

    public async Task<GroupRoleMapping?> GetGroupRoleMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
        => await _db.GroupRoleMappings
            .Include(g => g.Role)
            .FirstOrDefaultAsync(g => g.GroupName == groupName, ct);

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
        if (mapping is null)
        {
            return false;
        }

        _db.GroupRoleMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default)
    {
        var mappings = await _db.GroupRoleMappings
            .Include(g => g.Role)
            .AsNoTracking()
            .ToListAsync(ct);

        return mappings.ToDictionary(m => m.GroupName, m => m.Role.Name);
    }

    // ────────────────────────── Permission Policies ──────────────────────────

    public async Task<List<PermissionPolicy>> GetAllPermissionPoliciesAsync(CancellationToken ct = default)
        => await _db.PermissionPolicies.AsNoTracking().OrderBy(p => p.PolicyName).ToListAsync(ct);

    public async Task<PermissionPolicy?> GetPermissionPolicyByNameAsync(string policyName, CancellationToken ct = default)
        => await _db.PermissionPolicies.FirstOrDefaultAsync(p => p.PolicyName == policyName, ct);

    public async Task<PermissionPolicy> CreatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default)
    {
        _db.PermissionPolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<PermissionPolicy> UpdatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _db.PermissionPolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<bool> DeletePermissionPolicyAsync(int id, CancellationToken ct = default)
    {
        var policy = await _db.PermissionPolicies.FindAsync(new object[] { id }, ct);
        if (policy is null)
        {
            return false;
        }

        _db.PermissionPolicies.Remove(policy);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ────────────────────────── Role Policies ──────────────────────────

    public async Task<List<RolePolicy>> GetAllRolePoliciesAsync(CancellationToken ct = default)
        => await _db.RolePolicies.AsNoTracking().OrderBy(p => p.PolicyName).ToListAsync(ct);

    public async Task<RolePolicy?> GetRolePolicyByNameAsync(string policyName, CancellationToken ct = default)
        => await _db.RolePolicies.FirstOrDefaultAsync(p => p.PolicyName == policyName, ct);

    public async Task<RolePolicy> CreateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default)
    {
        _db.RolePolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<RolePolicy> UpdateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _db.RolePolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<bool> DeleteRolePolicyAsync(int id, CancellationToken ct = default)
    {
        var policy = await _db.RolePolicies.FindAsync(new object[] { id }, ct);
        if (policy is null)
        {
            return false;
        }

        _db.RolePolicies.Remove(policy);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
