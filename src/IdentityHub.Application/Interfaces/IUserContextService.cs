using IdentityHub.Domain.Models;
using System.Security.Claims;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for extracting and mapping user context from JWT claims
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Extract user context from claims principal
    /// </summary>
    /// <param name="claimsPrincipal">The authenticated user's claims</param>
    /// <returns>User context with identity information</returns>
    UserContext GetUserContext(ClaimsPrincipal claimsPrincipal);

    /// <summary>
    /// Validate that the user context is complete and valid
    /// </summary>
    /// <param name="userContext">User context to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateUserContext(UserContext userContext);
}
