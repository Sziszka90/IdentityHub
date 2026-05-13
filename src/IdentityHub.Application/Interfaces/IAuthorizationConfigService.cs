using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for managing authorization configuration: roles, permissions,
/// group-to-role mappings, permission policies, and role policies.
/// </summary>
public interface IAuthorizationConfigService
{
    // ── Full config snapshot ─────────────────────────────────────────────

    Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default);
    Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default);

    // ── Roles ────────────────────────────────────────────────────────────

    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Creates a role with the given permissions. Returns null if the name is already taken.</summary>
    Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default);

    /// <summary>Updates a role's description and permissions. Returns null if the role does not exist.</summary>
    Task<Role?> UpdateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default);

    /// <summary>Deletes a role by name. Returns false if the role does not exist.</summary>
    Task<bool> DeleteRoleAsync(string name, CancellationToken ct = default);

    // ── Group-Role Mappings ──────────────────────────────────────────────

    Task<List<GroupRoleMapping>> GetAllGroupMappingsAsync(CancellationToken ct = default);
    Task<GroupRoleMapping?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default);

    /// <summary>Creates a group-to-role mapping. Returns null if the group already has a mapping.</summary>
    Task<GroupRoleMapping?> CreateGroupMappingAsync(string groupName, int roleId, CancellationToken ct = default);

    /// <summary>Updates the role of an existing group mapping. Returns null if the mapping does not exist.</summary>
    Task<GroupRoleMapping?> UpdateGroupMappingAsync(int id, int roleId, CancellationToken ct = default);

    Task<bool> DeleteGroupMappingAsync(int id, CancellationToken ct = default);

}
