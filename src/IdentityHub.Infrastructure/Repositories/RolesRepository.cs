using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRolesRepository"/>.
/// </summary>
public class RolesRepository : IRolesRepository
{
    private readonly IdentityHubDbContext _db;

    public RolesRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default)
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
        _db.Roles.Update(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid id, CancellationToken ct = default)
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

    public async Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default)
        => await _db.GroupRoleMappings
            .Include(g => g.Role)
            .AsNoTracking()
            .OrderBy(g => g.GroupId)
            .ToListAsync(ct);

    public async Task<GroupRoleMapping?> GetGroupRoleMappingByGroupIdAsync(Guid groupId, CancellationToken ct = default)
        => await _db.GroupRoleMappings
            .Include(g => g.Role)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, ct);

    public async Task<GroupRoleMapping?> GetGroupRoleMappingByRoleIdAsync(Guid roleId, CancellationToken ct = default)
    {
        return await _db.GroupRoleMappings
            .Include(g => g.Role)
            .FirstOrDefaultAsync(g => g.RoleId == roleId, ct);
    }

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

    public async Task<bool> DeleteGroupRoleMappingAsync(Guid id, CancellationToken ct = default)
    {
        var mapping = await _db.GroupRoleMappings.FindAsync([id], ct);
        if (mapping is null)
        {
            return false;
        }

        _db.GroupRoleMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Dictionary<string, Role>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default)
    {
        var mappings = await _db.GroupRoleMappings
            .Include(g => g.Role)
            .AsNoTracking()
            .ToListAsync(ct);

        return mappings.ToDictionary(m => m.GroupId.ToString(), m => m.Role);
    }

    public async Task<List<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        return await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Where(r => roleIds.Contains(r.Id))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<GroupRoleMapping>> GetGroupRoleMappingsByGroupIdsAsync(IEnumerable<string> groupIds, CancellationToken ct = default)
    {
        var guids = groupIds
            .Select(id => Guid.TryParse(id, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        return await _db.GroupRoleMappings
            .Include(g => g.Role)
            .Where(g => guids.Contains(g.GroupId))
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
