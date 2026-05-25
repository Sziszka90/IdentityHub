using IdentityHub.Contracts.DTOs.GroupRoleMappings.Responses;
using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for managing roles and group-role mappings.
/// Delegates persistence to <see cref="IRolesRepository"/> and <see cref="IPermissionsRepository"/>.
/// </summary>
public interface IRoleService
{
    // -------------------------------------------------------------------------
    // User Role Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all roles assigned to a user by mapping their direct group memberships.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="Role"/> entities assigned to the user via direct group membership.</returns>
    Task<List<Role>> GetDirectRolesForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all roles assigned to a user by mapping their transitive group memberships (including nested groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="Role"/> entities assigned to the user via transitive group membership.</returns>
    Task<List<Role>> GetTransitiveRolesForUserAsync(string userId, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Roles CRUD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all roles in the system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all <see cref="Role"/> entities.</returns>
    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a role by its unique ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Role"/> or <c>null</c> if not found.</returns>
    Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new role with the specified name, description, and permissions.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="description">Role description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Role"/> or <c>null</c> if a role with the same name exists.</returns>
    Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing role's description and permissions.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Role"/> or <c>null</c> if not found.</returns>
    Task<Role?> UpdateRoleAsync(Guid roleId, string? description, List<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Deletes a role by ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Group-Role Mappings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all group-role mappings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of resolved <see cref="GroupRoleMapping"/> domain model.</returns>
    Task<List<GroupRoleMapping>> GetAllGroupMappingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a group-role mapping by group name.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching resolved <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    Task<GroupRoleMapping?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default);

    /// <summary>
    /// Gets a group-role mapping by role ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    Task<GroupRoleMapping?> GetGroupMappingByRoleIdAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new group-role mapping.
    /// </summary>
    /// <param name="groupId">Group name.</param>
    /// <param name="roleId">Role ID to map to the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="GroupRoleMapping"/> or <c>null</c> if a mapping for the group already exists.</returns>
    Task<GroupRoleMapping?> CreateGroupMappingAsync(Guid groupId, Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing group-role mapping.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="roleId">New role ID to assign to the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    Task<GroupRoleMapping?> UpdateGroupMappingAsync(Guid id, Guid groupId, Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a group-role mapping by ID.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteGroupMappingAsync(Guid id, CancellationToken ct = default);
}
