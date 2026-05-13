using Microsoft.Graph.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for Microsoft Graph API operations
/// </summary>
public interface IGraphService
{
    /// <summary>
    /// Create a new user in Microsoft Graph
    /// </summary>
    /// <param name="user">User object to create</param>
    /// <returns>The created User object</returns>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Update an existing user in Microsoft Graph
    /// </summary>
    /// <param name="userId">ID of the user to update</param>
    /// <param name="user">User object with updated fields</param>
    /// <returns>The updated User object</returns>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Delete a user from Microsoft Graph
    /// </summary>
    /// <param name="userId">ID of the user to delete</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteUserAsync(string userId);

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

    /// <summary>
    /// Create a new group in Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="displayName">Display name of the group</param>
    /// <param name="mailNickname">Mail nickname (unique alias)</param>
    /// <returns>The created Group object</returns>
    Task<Group> CreateGroupAsync(string displayName, string mailNickname);

    /// <summary>
    /// Update an existing group in Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="groupId">ID of the group to update</param>
    /// <param name="displayName">New display name</param>
    /// <param name="mailNickname">New mail nickname</param>
    /// <returns>The updated Group object</returns>
    Task<Group> UpdateGroupAsync(string groupId, string displayName, string mailNickname);

    /// <summary>
    /// Delete a group from Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="groupId">ID of the group to delete</param>
    Task DeleteGroupAsync(string groupId);

    /// <summary>
    /// Query groups in Microsoft Graph (Azure AD) by display name (optional)
    /// </summary>
    /// <param name="displayName">Display name to filter by (optional)</param>
    /// <returns>List of matching groups</returns>
    Task<List<Group>> QueryGroupsAsync(string? displayName = null);
}
