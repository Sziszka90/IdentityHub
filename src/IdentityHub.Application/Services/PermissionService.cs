using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of permission resolution service with caching
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly RolePermissionOptions _options;
    private readonly ICacheService _cacheService;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IOptions<RolePermissionOptions> options,
        ICacheService cacheService,
        IOptions<RedisCacheOptions> cacheOptions,
        ILogger<PermissionService> logger)
    {
        _options = options.Value;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Resolve permissions for given roles (with caching)
    /// </summary>
    public List<string> ResolvePermissions(IEnumerable<string> roles)
    {
        if (roles is null || !roles.Any())
        {
            return [];
        }

        var permissions = new HashSet<string>();

        foreach (string role in roles)
        {
            // Try to get from cache first
            var cacheKey = $"role:{role}:permissions";
            var cachedPermissions = _cacheService.GetAsync<List<string>>(cacheKey).Result;

            if (cachedPermissions != null)
            {
                _logger.LogDebug("Cache hit for role {Role} permissions", role);
                foreach (var permission in cachedPermissions)
                {
                    permissions.Add(permission);
                }
                continue;
            }

            // Cache miss - resolve from configuration
            if (_options.RolePermissions.TryGetValue(role, out List<string>? rolePermissions))
            {
                _logger.LogDebug("Cache miss for role {Role} permissions, caching now", role);

                // Add to result
                foreach (string permission in rolePermissions)
                {
                    permissions.Add(permission);
                }

                // Cache for future requests
                _ = _cacheService.SetAsync(
                    cacheKey,
                    rolePermissions,
                    _cacheOptions.RolePermissionsExpirationSeconds);
            }
        }

        return [.. permissions];
    }

    /// <summary>
    /// Map Entra ID groups to application roles
    /// </summary>
    public List<string> MapGroupsToRoles(IEnumerable<string> groups)
    {
        if (groups is null || !groups.Any())
        {
            return [];
        }

        var roles = new HashSet<string>();

        foreach (string group in groups)
        {
            if (_options.GroupToRoleMapping.TryGetValue(group, out string? role))
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
