using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing authorization configuration.
/// Encapsulates business logic for roles, group-role mappings, permission policies, and role policies.
/// </summary>
public class AuthorizationConfigService : IAuthorizationConfigService
{
    private readonly IAuthorizationConfigRepository _authorizationConfigRepository;

    public AuthorizationConfigService(
        IAuthorizationConfigRepository authorizationConfigRepository)
    {
        _authorizationConfigRepository = authorizationConfigRepository;
    }

    public Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default)
        => _authorizationConfigRepository.GetAllRolePermissionsAsync(ct);

    public Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default)
        => _authorizationConfigRepository.GetGroupToRoleDictionaryAsync(ct);

    // ── Roles ─────────────────────────────────────────────────────────────

    public Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => _authorizationConfigRepository.GetAllRolesAsync(ct);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => _authorizationConfigRepository.GetRoleByNameAsync(name, ct);

    public async Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        if (await _authorizationConfigRepository.GetRoleByNameAsync(name, ct) is not null)
            return null;

        var role = await _authorizationConfigRepository.CreateRoleAsync(new Role { Name = name, Description = description }, ct);

        if (permissions.Count > 0)
            await _authorizationConfigRepository.SetRolePermissionsAsync(role.Name, permissions, ct);

        return await _authorizationConfigRepository.GetRoleByNameAsync(role.Name, ct);
    }

    public async Task<Role?> UpdateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _authorizationConfigRepository.GetRoleByNameAsync(name, ct);
        if (role is null)
            return null;

        role.Description = description;
        await _authorizationConfigRepository.UpdateRoleAsync(role, ct);
        await _authorizationConfigRepository.SetRolePermissionsAsync(name, permissions, ct);

        return await _authorizationConfigRepository.GetRoleByNameAsync(name, ct);
    }

    public async Task<bool> DeleteRoleAsync(string name, CancellationToken ct = default)
    {
        var role = await _authorizationConfigRepository.GetRoleByNameAsync(name, ct);
        if (role is null)
            return false;

        return await _authorizationConfigRepository.DeleteRoleAsync(role.Id, ct);
    }

    // ── Group-Role Mappings ───────────────────────────────────────────────

    public Task<List<GroupRoleMapping>> GetAllGroupMappingsAsync(CancellationToken ct = default)
        => _authorizationConfigRepository.GetAllGroupRoleMappingsAsync(ct);

    public Task<GroupRoleMapping?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
        => _authorizationConfigRepository.GetGroupRoleMappingByGroupNameAsync(groupName, ct);

    public async Task<GroupRoleMapping?> CreateGroupMappingAsync(string groupName, int roleId, CancellationToken ct = default)
    {
        if (await _authorizationConfigRepository.GetGroupRoleMappingByGroupNameAsync(groupName, ct) is not null)
            return null;

        return await _authorizationConfigRepository.CreateGroupRoleMappingAsync(
            new GroupRoleMapping { GroupName = groupName, RoleId = roleId }, ct);
    }

    public async Task<GroupRoleMapping?> UpdateGroupMappingAsync(int id, int roleId, CancellationToken ct = default)
    {
        var mappings = await _authorizationConfigRepository.GetAllGroupRoleMappingsAsync(ct);
        var mapping = mappings.FirstOrDefault(m => m.Id == id);
        if (mapping is null)
            return null;

        mapping.RoleId = roleId;
        await _authorizationConfigRepository.UpdateGroupRoleMappingAsync(mapping, ct);
        return mapping;
    }

    public Task<bool> DeleteGroupMappingAsync(int id, CancellationToken ct = default)
        => _authorizationConfigRepository.DeleteGroupRoleMappingAsync(id, ct);

}
