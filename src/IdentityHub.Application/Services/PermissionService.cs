using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for resolving user permissions from roles and managing permission records.
/// Uses direct database access via <see cref="IPermissionsRepository"/> and <see cref="IRolesRepository"/>.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;
    private readonly IPermissionsRepository _permissionsRepository;
    private readonly IRolesRepository _rolesRepository;

    public PermissionService(
        ILogger<PermissionService> logger,
        IPermissionsRepository permissionsRepository,
        IRolesRepository rolesRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionsRepository = permissionsRepository ?? throw new ArgumentNullException(nameof(permissionsRepository));
        _rolesRepository = rolesRepository ?? throw new ArgumentNullException(nameof(rolesRepository));
    }

    // -------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolves the combined list of permission names for the given role names.
    /// </summary>
    /// <param name="roles">Role names to resolve permissions for.</param>
    /// <returns>Deduplicated list of permission names granted by any of the specified roles.</returns>
    public async Task<List<string>> ResolvePermissionsAsync(IEnumerable<string> roles)
    {
        if (roles is null || !roles.Any())
        {
            return [];
        }

        Dictionary<string, List<string>>? rolePermissionsMapping = null;
        try
        {
            rolePermissionsMapping = await _permissionsRepository.GetAllRolePermissionsAsync();
            _logger.LogDebug("Resolved role-permissions from database");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read role-permissions from DB");
        }

        var permissions = new HashSet<string>();
        if (rolePermissionsMapping is not null)
        {
            foreach (string role in roles)
            {
                if (rolePermissionsMapping.TryGetValue(role, out var dbPerms) && dbPerms is not null)
                {
                    foreach (string permission in dbPerms)
                    {
                        permissions.Add(permission);
                    }
                }
            }
        }

        return [.. permissions];
    }

    /// <summary>
    /// Maps Entra ID group claim values (names or object IDs) to application role names.
    /// </summary>
    /// <param name="groups">Group claim values from the user's token.</param>
    /// <returns>List of application role names that correspond to the given groups.</returns>
    public async Task<List<string>> MapGroupsToRolesAsync(IEnumerable<string> groups)
    {
        if (groups is null || !groups.Any())
        {
            return [];
        }

        Dictionary<string, string>? groupRoleMapping = null;
        try
        {
            groupRoleMapping = await _rolesRepository.GetGroupToRoleDictionaryAsync();
            _logger.LogDebug("Resolved group-role mapping from database");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read group-role mappings from DB");
        }

        var roles = new HashSet<string>();
        if (groupRoleMapping is not null)
        {
            foreach (string group in groups)
            {
                if (groupRoleMapping.TryGetValue(group, out var dbRole) && dbRole is not null)
                    roles.Add(dbRole);
            }
        }

        return [.. roles];
    }

    /// <summary>
    /// Checks whether a permission string matches a pattern (supports wildcard <c>.*</c>).
    /// </summary>
    /// <param name="permission">Permission to check (e.g., <c>"users.delete"</c>).</param>
    /// <param name="pattern">Pattern to match against (e.g., <c>"users.*"</c>).</param>
    /// <returns><c>true</c> if the permission matches the pattern; otherwise <c>false</c>.</returns>
    public bool MatchesPermission(string permission, string pattern)
    {
        if (string.IsNullOrEmpty(permission) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        if (permission.Equals(pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (pattern.EndsWith(".*"))
        {
            string prefix = pattern[..^2];
            return permission.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Permissions CRUD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all known permissions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all <see cref="Permission"/> entities.</returns>
    public Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => _permissionsRepository.GetAllPermissionsAsync(ct);

    /// <summary>
    /// Gets a permission by its unique name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Permission"/> or <c>null</c> if not found.</returns>
    public Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default)
        => _permissionsRepository.GetPermissionByNameAsync(name, ct);

    /// <summary>
    /// Creates a new permission with the specified name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Permission"/> or <c>null</c> if a permission with the same name already exists.</returns>
    public async Task<Permission?> CreatePermissionAsync(string name, CancellationToken ct = default)
    {
        if (await _permissionsRepository.GetPermissionByNameAsync(name, ct) is not null)
        {
            return null;
        }

        return await _permissionsRepository.CreatePermissionAsync(new Permission { Name = name }, ct);
    }

    /// <summary>
    /// Deletes a permission by name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public async Task<bool> DeletePermissionAsync(string name, CancellationToken ct = default)
    {
        var permission = await _permissionsRepository.GetPermissionByNameAsync(name, ct);
        if (permission is null)
        {
            return false;
        }

        return await _permissionsRepository.DeletePermissionAsync(permission.Id, ct);
    }
}
