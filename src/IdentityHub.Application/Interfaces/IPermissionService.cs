using IdentityHub.Domain.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for resolving permissions from roles
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Resolve permissions for given roles
    /// </summary>
    /// <param name="roles">List of role names</param>
    /// <returns>Combined list of permissions from all roles</returns>
    List<string> ResolvePermissions(IEnumerable<string> roles);

    /// <summary>
    /// Map Entra ID groups to application roles
    /// </summary>
    /// <param name="groups">List of Entra ID group names or IDs</param>
    /// <returns>List of application role names</returns>
    List<string> MapGroupsToRoles(IEnumerable<string> groups);

    /// <summary>
    /// Check if a permission matches a pattern (supports wildcards)
    /// </summary>
    /// <param name="permission">Permission to check (e.g., "users.delete")</param>
    /// <param name="pattern">Pattern to match (e.g., "users.*")</param>
    /// <returns>True if permission matches pattern</returns>
    bool MatchesPermission(string permission, string pattern);
}
