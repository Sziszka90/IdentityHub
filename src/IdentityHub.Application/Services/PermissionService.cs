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
    /// Resolves the combined list of permission names for the given roles.
    /// </summary>
    /// <param name="roles">Role entities to resolve permissions for.</param>
    /// <returns>Deduplicated list of permission names granted by any of the specified roles.</returns>
    public async Task<List<string>> ResolvePermissionsAsync(IEnumerable<Role> roles, CancellationToken cancellationToken = default)
    {
        var roleList = roles?.ToList();
        _logger.LogInformation("Resolving permissions for {RoleCount} role(s)", roleList?.Count ?? 0);

        if (roleList is null || roleList.Count == 0)
        {
            _logger.LogDebug("No roles were provided for permission resolution");
            return [];
        }

        // If RolePermissions are populated, use them; otherwise, fallback to DB mapping for test compatibility
        var permissions = new HashSet<string>();
        bool usedRolePermissions = false;
        foreach (var role in roleList)
        {
            if (role.RolePermissions != null && role.RolePermissions.Count > 0)
            {
                usedRolePermissions = true;
                foreach (var rp in role.RolePermissions)
                {
                    if (rp.Permission != null && !string.IsNullOrEmpty(rp.Permission.Name))
                    {
                        permissions.Add(rp.Permission.Name);
                    }
                }
            }
        }
        if (usedRolePermissions)
        {
            _logger.LogDebug("Resolved {PermissionCount} permission(s) directly from loaded role permissions", permissions.Count);
            return [.. permissions];
        }

        // Fallback: use DB mapping by role id.
        Dictionary<Guid, List<string>>? rolePermissionsMapping = null;
        try
        {
            rolePermissionsMapping = await _permissionsRepository.GetAllRolePermissionsAsync(cancellationToken);
            _logger.LogDebug("Resolved role-permissions from database");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read role-permissions from DB");
        }

        if (rolePermissionsMapping is not null)
        {
            foreach (var role in roleList)
            {
                if (rolePermissionsMapping.TryGetValue(role.Id, out var dbPerms) && dbPerms is not null)
                {
                    foreach (string permission in dbPerms)
                    {
                        permissions.Add(permission);
                    }
                }
            }
        }

        _logger.LogDebug("Resolved {PermissionCount} permission(s) from repository fallback", permissions.Count);
        return [.. permissions];
    }

    /// <summary>
    /// Maps Entra ID group claim values (names or object IDs) to application role names.
    /// </summary>
    /// <param name="groups">Group claim values from the user's token.</param>
    /// <returns>List of application role ids that correspond to the given groups.</returns>
    public async Task<List<Role>> MapGroupsToRolesAsync(IEnumerable<string> groupIds, CancellationToken cancellationToken = default)
    {
        var groupIdList = groupIds?.ToList();
        _logger.LogInformation("Mapping {GroupCount} group identifier(s) to roles", groupIdList?.Count ?? 0);

        if (groupIdList is null || groupIdList.Count == 0)
        {
            _logger.LogDebug("No group identifiers were provided for role mapping");
            return [];
        }

        HashSet<GroupRoleMapping> groupRoleMappings = [];

        try
        {
            foreach (var groupId in groupIdList)
            {
                if (Guid.TryParse(groupId, out var groupIdGuid))
                {
                    var map = await _rolesRepository.GetGroupRoleMappingByGroupIdAsync(groupIdGuid, cancellationToken);
                    if (map is not null)
                    {
                        groupRoleMappings.Add(map);
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping non-GUID group identifier {GroupId}", groupId);
                }
            }

            _logger.LogDebug("Resolved {MappingCount} group-role mapping(s) from repository", groupRoleMappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read group-role mappings from DB");
        }

        List<Role> roles = [.. groupRoleMappings
            .Select(m => m.Role)
            .Where(role => role is not null)
            .DistinctBy(role => role.Id)];

        _logger.LogDebug("Mapped group identifiers to {RoleCount} distinct role(s)", roles.Count);
        return roles;
    }

    /// <summary>
    /// Checks whether a permission string matches a pattern (supports wildcard <c>.*</c>).
    /// </summary>
    /// <param name="permission">Permission to check (e.g., <c>"users.delete"</c>).</param>
    /// <param name="pattern">Pattern to match against (e.g., <c>"users.*"</c>).</param>
    /// <returns><c>true</c> if the permission matches the pattern; otherwise <c>false</c>.</returns>
    public bool MatchesPermission(string permission, string pattern)
    {
        _logger.LogDebug("Checking whether permission {Permission} matches pattern {Pattern}", permission, pattern);

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
        _logger.LogInformation("Creating permission {PermissionName}", name);

        if (await _permissionsRepository.GetPermissionByNameAsync(name, ct) is not null)
        {
            _logger.LogWarning("Permission {PermissionName} already exists", name);
            return null;
        }

        var permission = await _permissionsRepository.CreatePermissionAsync(new Permission { Name = name }, ct);
        _logger.LogInformation("Created permission {PermissionName} with ID {PermissionId}", permission.Name, permission.Id);
        return permission;
    }

    /// <summary>
    /// Deletes a permission by name.
    /// </summary>
    /// <param name="name">Permission name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public async Task<bool> DeletePermissionAsync(string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting permission {PermissionName}", name);

        var permission = await _permissionsRepository.GetPermissionByNameAsync(name, ct);
        if (permission is null)
        {
            _logger.LogWarning("Permission {PermissionName} was not found for deletion", name);
            return false;
        }

        var deleted = await _permissionsRepository.DeletePermissionAsync(permission.Id, ct);
        _logger.LogInformation("Deletion of permission {PermissionName} completed with result {Deleted}", name, deleted);
        return deleted;
    }
}
