using IdentityHub.Application.Client;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of permission resolution service with caching.
/// Resolution priority:
///   1. <see cref="IIdentityHubClient"/> – remote IdentityHub.API (preferred for external apps).
///   2. <see cref="IPermissionsRepository"/> – direct DB access (used inside IdentityHub.API itself).
///   3. Appsettings-based <see cref="RolePermissionOptions"/> – static fallback.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly RolePermissionOptions _options;
    private readonly ICacheService _cacheService;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly ILogger<PermissionService> _logger;
    private readonly IPermissionsRepository _permissionsRepository;
    private readonly IRolesRepository _rolesRepository;
    private readonly IIdentityHubClient _client;

    public PermissionService(
        IOptions<RolePermissionOptions> options,
        ICacheService cacheService,
        IOptions<RedisCacheOptions> cacheOptions,
        ILogger<PermissionService> logger,
        IPermissionsRepository permissionsRepository,
        IRolesRepository rolesRepository,
        IIdentityHubClient client)
    {
        _options = options.Value;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _permissionsRepository = permissionsRepository;
        _rolesRepository = rolesRepository;
        _client = client;
    }

    /// <summary>
    /// Resolve permissions for given roles (with caching).
    /// Prefers <see cref="IIdentityHubClient"/> when registered; falls back to DB repo, then config.
    /// </summary>
    public async Task<List<string>> ResolvePermissions(IEnumerable<string> roles)
    {
        if (roles is null || !roles.Any())
        {
            return [];
        }

        Dictionary<string, List<string>>? rolePermissionsMapping = null;
        if (_client is not null)
        {
            try
            {
                rolePermissionsMapping = await _client.GetRolePermissionsAsync();
                _logger.LogDebug("Resolved role-permissions from IdentityHub client");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read role-permissions from IdentityHub client, falling back to DB/config");
            }
        }

        if (rolePermissionsMapping is null)
        {
            try
            {
                rolePermissionsMapping = await _permissionsRepository.GetAllRolePermissionsAsync();
                _logger.LogDebug("Resolved role-permissions from database");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read role-permissions from DB, falling back to config");
            }
        }

        var permissions = new HashSet<string>();

        foreach (string role in roles)
        {
            var cacheKey = $"role:{role}:permissions";
            var cachedPermissions = await _cacheService.GetAsync<List<string>>(cacheKey);

            if (cachedPermissions is not null)
            {
                _logger.LogDebug("Cache hit for role {Role} permissions", role);
                foreach (var permission in cachedPermissions)
                {
                    permissions.Add(permission);
                }
                continue;
            }

            List<string>? rolePermissions = null;

            // DB first, then config fallback
            if (rolePermissions is not null && rolePermissionsMapping!.TryGetValue(role, out var dbPerms))
            {
                rolePermissions = dbPerms;
                _logger.LogDebug("Resolved permissions for role {Role} from database", role);
            }
            else if (_options.RolePermissions.TryGetValue(role, out List<string>? configPerms))
            {
                rolePermissions = configPerms;
                _logger.LogDebug("Resolved permissions for role {Role} from config fallback", role);
            }

            if (rolePermissions is not null)
            {
                foreach (string permission in rolePermissions)
                {
                    permissions.Add(permission);
                }

                _ = _cacheService.SetAsync(
                    cacheKey,
                    rolePermissions,
                    _cacheOptions.RolePermissionsExpirationSeconds);
            }
        }

        return [.. permissions];
    }

    /// <summary>
    /// Map Entra ID groups to application roles.
    /// Prefers <see cref="IIdentityHubClient"/> when registered; falls back to DB repo, then config.
    /// </summary>
    public async Task<List<string>> MapGroupsToRoles(IEnumerable<string> groups)
    {
        if (groups is null || !groups.Any())
        {
            return [];
        }

        // 1. Try remote client (external apps)
        Dictionary<string, string>? groupRoleMapping = null;
        if (_client is not null)
        {
            try
            {
                groupRoleMapping = await _client.GetGroupToRoleMappingAsync();
                _logger.LogDebug("Resolved group-role mapping from IdentityHub client");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read group-role mappings from IdentityHub client, falling back to DB/config");
            }
        }

        // 2. Try DB-backed mapping
        if (groupRoleMapping is null && _permissionsRepository is not null)
        {
            try
            {
                groupRoleMapping = await _rolesRepository.GetGroupToRoleDictionaryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read group-role mappings from DB, falling back to config");
            }
        }

        var roles = new HashSet<string>();

        foreach (string group in groups)
        {
            string? role = null;

            // DB first, then config fallback
            if (groupRoleMapping is not null && groupRoleMapping.TryGetValue(group, out var dbRole))
            {
                role = dbRole;
            }
            else if (_options.GroupToRoleMapping.TryGetValue(group, out string? configRole))
            {
                role = configRole;
            }

            if (role is not null)
            {
                roles.Add(role);
            }
        }

        return [.. roles];
    }

    /// <summary>
    /// Check if a permission matches a pattern (supports wildcards)
    /// </summary>
    public bool MatchesPermission(string permission, string pattern)
    {
        if (string.IsNullOrEmpty(permission) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Exact match
        if (permission.Equals(pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Wildcard match (e.g., "users.*" matches "users.read")
        if (pattern.EndsWith(".*"))
        {
            string prefix = pattern.Substring(0, pattern.Length - 2);
            return permission.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
