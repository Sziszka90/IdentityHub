using System.Security.Claims;
using IdentityHub.Domain.Models;
using IdentityHub.Application.Interfaces;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of user context extraction from JWT claims
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IPermissionService _permissionService;

    public UserContextService(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Extract user context from JWT token claims
    /// </summary>
    public UserContext GetUserContext(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return new UserContext { IsAuthenticated = false };
        }

        var userContext = new UserContext
        {
            IsAuthenticated = true,
            UserId = GetClaimValue(claimsPrincipal, "oid") ?? GetClaimValue(claimsPrincipal, ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = GetClaimValue(claimsPrincipal, "preferred_username") ?? GetClaimValue(claimsPrincipal, ClaimTypes.Email) ?? string.Empty,
            DisplayName = GetClaimValue(claimsPrincipal, "name") ?? GetClaimValue(claimsPrincipal, ClaimTypes.Name) ?? string.Empty,
            TenantId = GetClaimValue(claimsPrincipal, "tid") ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        // Extract roles from token
        var tokenRoles = claimsPrincipal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .ToList();

        // Extract groups (if present in token)
        userContext.Groups = claimsPrincipal.Claims
            .Where(c => c.Type == "groups")
            .Select(c => c.Value)
            .ToList();

        // Map groups to application roles
        var rolesFromGroups = _permissionService.MapGroupsToRoles(userContext.Groups);

        // Combine token roles and mapped roles (union)
        var allRoles = tokenRoles.Concat(rolesFromGroups).Distinct().ToList();
        userContext.Roles = allRoles;

        // Resolve permissions from roles
        userContext.Permissions = _permissionService.ResolvePermissions(allRoles);

        // Store all claims for debugging/auditing
        userContext.Claims = claimsPrincipal.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(c => c.Value))
            );

        return userContext;
    }

    /// <summary>
    /// Validate that user context has required fields
    /// </summary>
    public bool ValidateUserContext(UserContext userContext)
    {
        return userContext.IsAuthenticated
            && !string.IsNullOrEmpty(userContext.UserId)
            && !string.IsNullOrEmpty(userContext.TenantId);
    }

    /// <summary>
    /// Helper method to safely get claim values
    /// </summary>
    private static string? GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}
