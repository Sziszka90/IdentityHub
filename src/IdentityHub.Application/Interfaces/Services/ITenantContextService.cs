using IdentityHub.Domain.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for managing tenant context.
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Gets the current tenant context from the active HTTP request.
    /// </summary>
    /// <returns>The <see cref="TenantContext"/> for the current request, or an empty context if unavailable.</returns>
    TenantContext GetTenantContext();

    /// <summary>
    /// Checks whether the specified user belongs to the specified tenant.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="tenantId">The tenant's unique identifier.</param>
    /// <returns><c>true</c> if the current context matches the given user and tenant; otherwise <c>false</c>.</returns>
    bool UserBelongsToTenant(string userId, string tenantId);
}
