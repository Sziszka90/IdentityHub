using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of permission resolution service
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly RolePermissionOptions _options;

    public PermissionService(IOptions<RolePermissionOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Resolve permissions for given roles
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
            if (_options.RolePermissions.TryGetValue(role, out List<string>? rolePermissions))
            {
                foreach (string permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
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
