using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository for managing authorization configuration (roles, group-role mappings, etc.)
/// </summary>
public interface IAuthorizationConfigRepository
{
    Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default);
    Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default);
    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);
    Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(int id, CancellationToken ct = default);
    Task SetRolePermissionsAsync(string roleName, List<string> permissions, CancellationToken ct = default);
    Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default);
    Task<GroupRoleMapping?> GetGroupRoleMappingByGroupNameAsync(string groupName, CancellationToken ct = default);
    Task<GroupRoleMapping> CreateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);
    Task<GroupRoleMapping> UpdateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);
    Task<bool> DeleteGroupRoleMappingAsync(int id, CancellationToken ct = default);
}
