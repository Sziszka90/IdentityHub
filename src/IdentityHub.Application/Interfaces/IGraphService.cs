using Microsoft.Graph.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for Microsoft Graph API operations
/// </summary>
public interface IGraphService
{
    /// <summary>
    /// Get user profile by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile or null if not found</returns>
    Task<User?> GetUserAsync(string userId);

    /// <summary>
    /// Get user's group memberships
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of group IDs</returns>
    Task<List<string>> GetUserGroupsAsync(string userId);

    /// <summary>
    /// Get user's transitive group memberships (includes nested groups)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of group IDs</returns>
    Task<List<string>> GetUserTransitiveGroupsAsync(string userId);

    /// <summary>
    /// Get group by ID
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Group or null if not found</returns>
    Task<Group?> GetGroupAsync(string groupId);

    /// <summary>
    /// Get all users in tenant (paginated)
    /// </summary>
    /// <param name="top">Number of results to return</param>
    /// <param name="skip">Number of results to skip</param>
    /// <returns>List of users</returns>
    Task<List<User>> GetUsersAsync(int top = 100, int skip = 0);

    /// <summary>
    /// Get group members
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>List of user IDs</returns>
    Task<List<string>> GetGroupMembersAsync(string groupId);

    /// <summary>
    /// Check if Graph API is available
    /// </summary>
    /// <returns>True if Graph API is configured and accessible</returns>
    Task<bool> IsAvailableAsync();
}
