using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of permission resolution service with caching.
/// Reads role-permission and group-role mappings from the database
/// via <see cref="IAuthorizationConfigRepository"/>, with a fallback
/// to appsettings-based <see cref="RolePermissionOptions"/> when
/// the repository is not registered.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly RolePermissionOptions _options;
    private readonly ICacheService _cacheService;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly ILogger<PermissionService> _logger;
    private readonly IAuthorizationConfigRepository? _repo;

    public PermissionService(
        IOptions<RolePermissionOptions> options,
        ICacheService cacheService,
        IOptions<RedisCacheOptions> cacheOptions,
        ILogger<PermissionService> logger,
        IAuthorizationConfigRepository? repo = null)
    {
        _options = options.Value;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _repo = repo;
    }

    /// <summary>
    /// Resolve permissions for given roles (with caching).
    /// Prefers database source when available.
    /// </summary>
    public List<string> ResolvePermissions(IEnumerable<string> roles)
    {
        if (roles is null || !roles.Any())
        {
            return [];
        }

        // Try DB-backed resolution
        Dictionary<string, List<string>>? dbRolePermissions = null;
        if (_repo is not null)
        {
            try
            {
                dbRolePermissions = _repo.GetAllRolePermissionsAsync().GetAwaiter().GetResult();
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
            var cachedPermissions = _cacheService.GetAsync<List<string>>(cacheKey).Result;

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
            if (dbRolePermissions is not null && dbRolePermissions.TryGetValue(role, out var dbPerms))
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
    /// Prefers database source when available.
    /// </summary>
    public List<string> MapGroupsToRoles(IEnumerable<string> groups)
    {
        if (groups is null || !groups.Any())
        {
            return [];
        }

        // Try DB-backed mapping
        Dictionary<string, string>? dbGroupMapping = null;
        if (_repo is not null)
        {
            try
            {
                dbGroupMapping = _repo.GetGroupToRoleDictionaryAsync().GetAwaiter().GetResult();
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
            if (dbGroupMapping is not null && dbGroupMapping.TryGetValue(group, out var dbRole))
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
