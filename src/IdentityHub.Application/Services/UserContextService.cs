using System.Security.Claims;
using IdentityHub.Domain.Models;
using IdentityHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Implementation of user context extraction from JWT claims
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UserContextService> _logger;
    private readonly ITenantContextService _tenantContextService;

    public UserContextService(
        IPermissionService permissionService,
        ILogger<UserContextService> logger,
        ITenantContextService tenantContextService)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContextService = tenantContextService ?? throw new ArgumentNullException(nameof(tenantContextService));
    }

    /// <summary>
    /// Extracts user context (identity, roles, permissions) from a claims principal.
    /// </summary>
    /// <param name="claimsPrincipal">The authenticated user's claims principal.</param>
    /// <returns>A populated <see cref="UserContext"/>; <see cref="UserContext.IsAuthenticated"/> is <c>false</c> if authentication failed.</returns>
    public async Task<UserContext> GetUserContext(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity?.IsAuthenticated is not true)
        {
            return new UserContext { IsAuthenticated = false };
        }

        var tenantId = _tenantContextService.GetTenantContext().TenantId;

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Missing tenant ID in token claims");
            return new UserContext { IsAuthenticated = false };
        }

        var userContext = new UserContext
        {
            IsAuthenticated = true,
            UserId = GetClaimValue(claimsPrincipal, "oid") ?? GetClaimValue(claimsPrincipal, ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = GetClaimValue(claimsPrincipal, "preferred_username") ?? GetClaimValue(claimsPrincipal, ClaimTypes.Email) ?? string.Empty,
            DisplayName = GetClaimValue(claimsPrincipal, "name") ?? GetClaimValue(claimsPrincipal, ClaimTypes.Name) ?? string.Empty,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var tokenRoles = claimsPrincipal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .ToList();

        userContext.Groups = [.. claimsPrincipal.Claims
            .Where(c => c.Type == "groups")
            .Select(c => c.Value)];

        var rolesFromGroups = await _permissionService.MapGroupsToRolesAsync(userContext.Groups);

        var allRoles = tokenRoles.Concat(rolesFromGroups).Distinct().ToList();
        userContext.Roles = allRoles;

        userContext.Permissions = await _permissionService.ResolvePermissionsAsync(allRoles);

        userContext.Claims = claimsPrincipal.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(c => c.Value))
            );

        return userContext;
    }

    /// <summary>
    /// Validates that the user context is complete and authenticated.
    /// </summary>
    /// <param name="userContext">User context to validate.</param>
    /// <returns><c>true</c> if the context is authenticated and contains a non-empty user ID and tenant ID; otherwise <c>false</c>.</returns>
    public bool ValidateUserContext(UserContext userContext)
    {
        return userContext.IsAuthenticated
            && !string.IsNullOrEmpty(userContext.UserId)
            && !string.IsNullOrEmpty(userContext.TenantId);
    }

    /// <summary>
    /// Safely retrieves a single claim value from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal to search.</param>
    /// <param name="claimType">The claim type to look up.</param>
    /// <returns>The claim value, or <c>null</c> if not present.</returns>
    private static string? GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}
