using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository abstraction for managing permissions and role→permission mappings.
/// Implementations provide CRUD operations for <see cref="Permission"/> entities
/// and helper methods to read or replace permissions assigned to roles.
/// </summary>
public interface IPermissionsRepository
{
    /// <summary>
    /// Returns all known permissions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all <see cref="Permission"/> entities.</returns>
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a permission by its numeric identifier.
    /// </summary>
    /// <param name="id">Database identifier of the permission.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Permission"/> or <c>null</c> if not found.</returns>
    Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a permission by its unique name.
    /// </summary>
    /// <param name="name">Permission name (case-sensitive as stored).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Permission"/> or <c>null</c> if not found.</returns>
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Creates a new permission record.
    /// </summary>
    /// <param name="permission">Permission entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Permission"/> with its assigned id.</returns>
    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default);

    /// <summary>
    /// Deletes a permission by id.
    /// </summary>
    /// <param name="id">Identifier of the permission to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the permission was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeletePermissionAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Get all permission names assigned to a role.
    /// The result contains only the permission names (not full entities).
    /// </summary>
    /// <param name="roleName">Role name to query permissions for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of permission names associated with the role.</returns>
    Task<List<string>> GetPermissionsForRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Replace the full permission set for a role. Any permission names that do not
    /// exist will be created.
    /// </summary>
    /// <param name="roleName">Target role name.</param>
    /// <param name="permissions">List of permission names to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetRolePermissionsAsync(string roleName, List<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Returns a materialized dictionary of all roles to their permission name lists.
    /// Useful for bulk lookups and caching scenarios.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary where the key is the role name and the value is the list of permission names.</returns>
    Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default);

}
