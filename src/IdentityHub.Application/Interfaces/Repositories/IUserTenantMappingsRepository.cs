using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository abstraction for persisted user-to-tenant mappings.
/// </summary>
public interface IUserTenantMappingsRepository
{
    /// <summary>
    /// Gets all user mappings visible to the current tenant.
    /// </summary>
    Task<List<UserTenantMapping>> GetAllUserTenantMappingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a mapping by Graph user ID synchronously.
    /// </summary>
    UserTenantMapping? GetUserTenantMappingByUserId(string userId);

    /// <summary>
    /// Ensures the given user ID is stored for the current tenant.
    /// </summary>
    Task<UserTenantMapping> UpsertUserTenantMappingAsync(string userId, CancellationToken ct = default);
}
