using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Repository abstraction for managing role-based authorization policies.
/// Implementations persist and retrieve <see cref="RolePolicy"/> entities.
/// </summary>
public interface IRolePoliciesRepository
{
    /// <summary>
    /// Retrieves all role policies from the store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="RolePolicy"/> instances.</returns>
    Task<List<RolePolicy>> GetAllRolePoliciesAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a role policy by its unique policy name.
    /// </summary>
    /// <param name="policyName">The policy name to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="RolePolicy"/> or <c>null</c> if not found.</returns>
    Task<RolePolicy?> GetRolePolicyByNameAsync(string policyName, CancellationToken ct = default);

    /// <summary>
    /// Creates a new role policy in the store.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="RolePolicy"/> with any assigned identifiers.</returns>
    Task<RolePolicy> CreateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing role policy.
    /// </summary>
    /// <param name="policy">The updated policy entity. Its identifier is used to locate the stored record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="RolePolicy"/> instance.</returns>
    Task<RolePolicy> UpdateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Deletes a role policy by id.
    /// </summary>
    /// <param name="id">Identifier of the role policy to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the role policy was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteRolePolicyAsync(int id, CancellationToken ct = default);
}
