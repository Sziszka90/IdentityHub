using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository abstraction for managing permission and role-based authorization policies.
/// Implementations persist and retrieve <see cref="PermissionPolicy"/> and role policy entities.
/// </summary>
public interface IPermissionPoliciesRepository
{
    /// <summary>
    /// Retrieves all permission policies from the store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="PermissionPolicy"/> instances.</returns>
    Task<List<PermissionPolicy>> GetAllPermissionPoliciesAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a permission policy by its unique policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="PermissionPolicy"/> or <c>null</c> if not found.</returns>
    Task<PermissionPolicy?> GetPermissionPolicyByNameAsync(string policyName, CancellationToken ct = default);

    /// <summary>
    /// Creates a new permission policy in the store.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="PermissionPolicy"/> with any assigned identifiers.</returns>
    Task<PermissionPolicy> CreatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission policy.
    /// </summary>
    /// <param name="policy">The updated policy entity. Its identifier is used to locate the stored record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="PermissionPolicy"/> instance.</returns>
    Task<PermissionPolicy> UpdatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Deletes a permission policy by id.
    /// </summary>
    /// <param name="id">Identifier of the permission policy to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the policy was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeletePermissionPolicyAsync(int id, CancellationToken ct = default);
}
