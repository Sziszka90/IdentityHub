using IdentityHub.Domain.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for admin operations
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Get all users with their effective permissions (tenant-scoped)
    /// </summary>
    /// <returns>List of users with permissions</returns>
    Task<List<UserPermissionsDto>> GetUsersWithPermissionsAsync();

    /// <summary>
    /// Get a specific user's effective permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User permissions or null if not found</returns>
    Task<UserPermissionsDto?> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Get detailed permission resolution chain for a user
    /// Shows groups → roles → permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Permission resolution chain or null if not found</returns>
    Task<PermissionResolutionChainDto?> GetPermissionResolutionChainAsync(string userId);

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    /// <returns>List of roles and permissions</returns>
    List<RolePermissionsDto> GetAllRolesWithPermissions();

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Role permissions or null if role not found</returns>
    RolePermissionsDto? GetRolePermissions(string roleName);

    /// <summary>
    /// Create a new role with permissions
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="permissions">List of permissions</param>
    /// <returns>Created role or null if role already exists</returns>
    Task<RolePermissionsDto?> CreateRoleAsync(string roleName, List<string> permissions);

    /// <summary>
    /// Update an existing role's permissions
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="permissions">New list of permissions</param>
    /// <returns>Updated role or null if role not found</returns>
    Task<RolePermissionsDto?> UpdateRolePermissionsAsync(string roleName, List<string> permissions);

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteRoleAsync(string roleName);

    /// <summary>
    /// Assign roles to a user (via group membership in production)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">List of role names</param>
    /// <returns>Updated user permissions</returns>
    Task<UserPermissionsDto?> AssignRolesToUserAsync(string userId, List<string> roles);

    /// <summary>
    /// Remove roles from a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">List of role names</param>
    /// <returns>Updated user permissions</returns>
    Task<UserPermissionsDto?> RemoveRolesFromUserAsync(string userId, List<string> roles);
}
