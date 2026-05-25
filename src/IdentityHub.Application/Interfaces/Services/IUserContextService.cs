using System.Security.Claims;
using System.Threading;
using IdentityHub.Domain.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for extracting and mapping user context from JWT claims.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Extracts user context (identity, roles, permissions) from a claims principal.
    /// </summary>
    /// <param name="claimsPrincipal">The authenticated user's claims principal.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A populated <see cref="UserContext"/>; <see cref="UserContext.IsAuthenticated"/> is <c>false</c> if authentication failed.</returns>
    Task<UserContext> GetUserContext(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the user context is complete and authenticated.
    /// </summary>
    /// <param name="userContext">User context to validate.</param>
    /// <returns><c>true</c> if the context is authenticated and contains a non-empty user ID and tenant ID; otherwise <c>false</c>.</returns>
    bool ValidateUserContext(UserContext userContext);
}
