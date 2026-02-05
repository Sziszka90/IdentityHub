using IdentityHub.Domain.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for managing tenant context
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Get tenant context from HTTP context
    /// </summary>
    TenantContext GetTenantContext();

    /// <summary>
    /// Check if user belongs to tenant
    /// </summary>
    bool UserBelongsToTenant(string userId, string tenantId);
}
