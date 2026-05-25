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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of user and tenant mappings.</returns>

    Task<List<UserTenantMapping>> GetAllUserTenantMappingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a mapping by Graph user ID synchronously.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <returns>User and tenant mapping.</returns>
    UserTenantMapping? GetUserTenantMappingByUserId(string userId);

    /// <summary>
    /// Ensures the given user ID is stored for the current tenant.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User and tenant mapping.</returns>
    Task<UserTenantMapping> UpsertUserTenantMappingAsync(string userId, CancellationToken ct = default);
}
