using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository for managing authorization configuration in the database.
/// Covers roles, permissions, group-role mappings, and authorization policies.
/// </summary>
public interface IAuthorizationConfigRepository
{
    // ── Roles ──

    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);
    Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(int id, CancellationToken ct = default);

    // ── Permissions ──

    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<Permission?> GetPermissionByIdAsync(int id, CancellationToken ct = default);
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default);
    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default);
    Task<bool> DeletePermissionAsync(int id, CancellationToken ct = default);

    // ── Role-Permission mappings ──

    /// <summary>
    /// Get all permissions assigned to a role (includes Permission navigation).
    /// </summary>
    Task<List<string>> GetPermissionsForRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Replace the full permission set for a role.
    /// Creates any new Permission rows as needed.
    /// </summary>
    Task SetRolePermissionsAsync(string roleName, List<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Get all roles with their permissions (materialized).
    /// </summary>
    Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default);

    // ── Group-Role mappings ──

    Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default);
    Task<GroupRoleMapping?> GetGroupRoleMappingByGroupNameAsync(string groupName, CancellationToken ct = default);
    Task<GroupRoleMapping> CreateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);
    Task<GroupRoleMapping> UpdateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);
    Task<bool> DeleteGroupRoleMappingAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get the group-to-role dictionary (groupName → roleName).
    /// </summary>
    Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default);

    // ── Permission Policies ──

    Task<List<PermissionPolicy>> GetAllPermissionPoliciesAsync(CancellationToken ct = default);
    Task<PermissionPolicy?> GetPermissionPolicyByNameAsync(string policyName, CancellationToken ct = default);
    Task<PermissionPolicy> CreatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default);
    Task<PermissionPolicy> UpdatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default);
    Task<bool> DeletePermissionPolicyAsync(int id, CancellationToken ct = default);

    // ── Role Policies ──

    Task<List<RolePolicy>> GetAllRolePoliciesAsync(CancellationToken ct = default);
    Task<RolePolicy?> GetRolePolicyByNameAsync(string policyName, CancellationToken ct = default);
    Task<RolePolicy> CreateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default);
    Task<RolePolicy> UpdateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default);
    Task<bool> DeleteRolePolicyAsync(int id, CancellationToken ct = default);
}
