using IdentityHub.Contracts.DTOs.Permissions.Responses;
using IdentityHub.Contracts.DTOs.Identity.Responses;

namespace IdentityHub.Client;

/// <summary>
/// Abstraction over the HTTP client that calls the central IdentityHub.API.
/// </summary>
public interface IIdentityHubClient
{
    // -------------------------------------------------------------------------
    // Authorization config (cached, M2M)
    // -------------------------------------------------------------------------

    /// <summary>Returns the role → permissions mapping (roleName → list of permission names).</summary>
    Task<Dictionary<string, List<string>>> GetRolePermissionsAsync(CancellationToken ct = default);

    /// <summary>Returns the group → role mapping (groupName → roleName).</summary>
    Task<Dictionary<string, string>> GetGroupToRoleMappingAsync(CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // AuthorizationController  (POST /api/authorization/check)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether the bearer-token user has the given permission.
    /// </summary>
    /// <param name="permission">The permission name to test (e.g. "users.read").</param>
    /// <param name="bearerToken">The user's JWT bearer token.</param>
    Task<PermissionCheckResponse> CheckPermissionAsync(string permission, string bearerToken, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // IdentityController  (GET /api/identity/...)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the full identity context for the bearer-token user
    /// (GET /api/identity/me).
    /// </summary>
    /// <param name="bearerToken">The user's JWT bearer token.</param>
    Task<UserContextResponse> GetCurrentUserAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>
    /// Returns a lightweight authentication status for the bearer-token user
    /// (GET /api/identity/status).
    /// </summary>
    /// <param name="bearerToken">The user's JWT bearer token.</param>
    Task<AuthStatusResponse> GetAuthStatusAsync(string bearerToken, CancellationToken ct = default);
}
