using IdentityHub.Application.DTOs.Permissions;
using IdentityHub.Application.DTOs.Users;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for user operations: querying permissions via Microsoft Graph and managing role assignments.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets all users with their effective permissions (tenant-scoped).
    /// </summary>
    /// <returns>List of users with their resolved groups, roles, and permissions.</returns>
    Task<List<UserPermissionsDto>> GetUsersWithPermissionsAsync();

    /// <summary>
    /// Gets a specific user's effective permissions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user's permissions DTO, or <c>null</c> if the user was not found.</returns>
    Task<UserPermissionsDto?> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Gets the detailed group → role → permission resolution chain for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The resolution chain DTO, or <c>null</c> if the user was not found.</returns>
    Task<PermissionResolutionChainDto?> GetPermissionResolutionChainAsync(string userId);

    /// <summary>
    /// Creates a new user and assigns roles via group membership.
    /// </summary>
    /// <param name="user">User entity to create (Microsoft Graph model).</param>
    /// <param name="roleIds">List of role IDs to assign via Azure AD group membership.</param>
    /// <returns>The created <see cref="User"/> object.</returns>
    Task<User?> CreateUserWithRolesAsync(User user, List<string> roleIds);

    /// <summary>
    /// Assigns roles to a user via Azure AD group membership by role IDs.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to assign.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user or any role was not found.</returns>
    Task<UserPermissionsDto?> AssignRolesToUserAsync(string userId, List<string> roleIds);

    /// <summary>
    /// Removes roles from a user by removing them from the corresponding Azure AD groups.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to remove.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user was not found.</returns>
    Task<UserPermissionsDto?> RemoveRolesFromUserAsync(string userId, List<string> roleIds);
}
