using System.Threading;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using IdentityHub.Contracts.DTOs.Users.Responses;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for user operations: querying permissions via Microsoft Graph and managing role assignments.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    Task<bool> UserHasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with their effective permissions (tenant-scoped).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>List of users with their resolved groups, roles, and permissions.</returns>
    Task<List<UserResponse>> GetUsersWithPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user's effective permissions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The user's permissions DTO, or <c>null</c> if the user was not found.</returns>
    Task<UserResponse?> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the detailed group → role → permission resolution chain for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The resolution chain DTO, or <c>null</c> if the user was not found.</returns>
    Task<PermissionResolutionChainResponse?> GetPermissionResolutionChainAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user and assigns roles via group membership.
    /// </summary>
    /// <param name="user">User entity to create (Microsoft Graph model).</param>
    /// <param name="roleIds">List of role IDs to assign via Azure AD group membership.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The created <see cref="User"/> object.</returns>
    Task<User?> CreateUserWithRolesAsync(User user, List<string> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's profile and adjusts their Azure AD group memberships
    /// based on the supplied role IDs. Only adds the user to groups they do not already belong to.
    /// </summary>
    /// <param name="user">User entity with updated fields (must have <see cref="User.Id"/> set).</param>
    /// <param name="roleIds">Desired list of role IDs to reflect via group membership.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated <see cref="User"/> object, or <c>null</c> if the update failed.</returns>
    Task<User?> UpdateUserWithRolesAsync(User user, string userId, List<string> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns roles to a user via Azure AD group membership by role IDs.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to assign.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user or any role was not found.</returns>
    Task<UserResponse?> AssignRolesToUserAsync(string userId, List<string> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes roles from a user by removing them from the corresponding Azure AD groups.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to remove.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user was not found.</returns>
    Task<UserResponse?> RemoveRolesFromUserAsync(string userId, List<string> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    /// <param name="response">Resolved user permissions response.</param>
    /// <param name="requiredPermission">Permission to evaluate.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    bool HasPermission(UserResponse? response, string requiredPermission);
}
