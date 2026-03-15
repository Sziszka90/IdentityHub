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
}
