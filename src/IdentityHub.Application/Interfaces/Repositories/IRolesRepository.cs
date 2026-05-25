using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository abstraction for managing roles and group→role mappings.
/// Implementations provide CRUD operations for <see cref="Role"/> entities
/// and management of <see cref="GroupRoleMapping"/> records.
/// </summary>
public interface IRolesRepository
{
    /// <summary>
    /// Retrieves all roles.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="Role"/> entities.</returns>
    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a role by its database identifier.
    /// </summary>
    /// <param name="id">Numeric identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Role"/> or <c>null</c> if not found.</returns>
    Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    /// <param name="name">Name of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Role"/> or <c>null</c> if not found.</returns>
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="role">Role entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Role"/> with its assigned id.</returns>
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="role">Updated role entity. Its id is used to locate the stored record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Role"/>.</returns>
    Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Deletes a role by id.
    /// </summary>
    /// <param name="id">Identifier of the role to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the role was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteRoleAsync(Guid id, CancellationToken ct = default);

    // ── Group-Role mappings ──

    /// <summary>
    /// Retrieves all configured group→role mappings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="GroupRoleMapping"/> entities.</returns>
    Task<List<GroupRoleMapping>> GetAllGroupRoleMappingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a group→role mapping by the group's name or id value.
    /// </summary>
    /// <param name="groupId">Group claim value (name or id) to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    Task<GroupRoleMapping?> GetGroupRoleMappingByGroupIdAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>
    /// Finds a group→role mapping by the role's unique identifier (roleId).
    /// </summary>
    /// <param name="roleId">Role unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    Task<GroupRoleMapping?> GetGroupRoleMappingByRoleIdAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new group→role mapping.
    /// </summary>
    /// <param name="mapping">Mapping entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="GroupRoleMapping"/> with its assigned id.</returns>
    Task<GroupRoleMapping> CreateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing group→role mapping.
    /// </summary>
    /// <param name="mapping">Updated mapping entity. Its id is used to locate the stored record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="GroupRoleMapping"/>.</returns>
    Task<GroupRoleMapping> UpdateGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct = default);

    /// <summary>
    /// Deletes a group→role mapping by id.
    /// </summary>
    /// <param name="id">Identifier of the mapping to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the mapping was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteGroupRoleMappingAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves roles by a list of role IDs.
    /// </summary>
    /// <param name="roleIds">List of role GUIDs to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching Role entities.</returns>
    Task<List<Role>> GetRolesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default);

    /// <summary>
    /// Retrieves group-role mappings for a given list of group IDs (names).
    /// </summary>
    /// <param name="groupIds">List of group IDs (names) to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching GroupRoleMapping entities.</returns>
    Task<List<GroupRoleMapping>> GetGroupRoleMappingsByGroupIdsAsync(IEnumerable<string> groupIds, CancellationToken ct = default);
}
