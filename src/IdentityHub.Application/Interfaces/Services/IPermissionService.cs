
using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for resolving user permissions from roles and managing permission records.
/// </summary>
public interface IPermissionService
{
    // -------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolves the combined list of permission names for the given role names.
    /// </summary>
    /// <param name="roles">Role names to resolve permissions for.</param>
    /// <returns>Deduplicated list of permission names granted by any of the specified roles.</returns>
    Task<List<string>> ResolvePermissionsAsync(IEnumerable<Role> roles);

    /// <summary>
    /// Maps Entra ID group claim values (names or object IDs) to application role names.
    /// </summary>
    /// <param name="groups">Group claim values from the user's token.</param>
    /// <returns>List of application roles.</returns>
    Task<List<Role>> MapGroupsToRolesAsync(IEnumerable<string> groups);

    /// <summary>
    /// Checks whether a permission string matches a pattern (supports wildcard <c>.*</c>).
    /// </summary>
    /// <param name="permission">Permission to check (e.g., <c>"users.delete"</c>).</param>
    /// <param name="pattern">Pattern to match against (e.g., <c>"users.*"</c>).</param>
    /// <returns><c>true</c> if the permission matches the pattern; otherwise <c>false</c>.</returns>
    bool MatchesPermission(string permission, string pattern);

    // -------------------------------------------------------------------------
    // Permissions CRUD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all known permissions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all <see cref="Permission"/> entities.</returns>
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a permission by its unique name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Permission"/> or <c>null</c> if not found.</returns>
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Creates a new permission with the specified name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Permission"/> or <c>null</c> if a permission with the same name already exists.</returns>
    Task<Permission?> CreatePermissionAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Deletes a permission by name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeletePermissionAsync(string name, CancellationToken ct = default);
}
