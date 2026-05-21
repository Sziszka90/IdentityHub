using Microsoft.Graph.Models;

namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for Microsoft Graph API operations.
/// </summary>
public interface IGraphService
{
    // -------------------------------------------------------------------------
    // Users
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get all users in the tenant (paginated).
    /// </summary>
    /// <param name="top">Maximum number of users to return (default: 100).</param>
    /// <param name="skip">Number of users to skip for pagination (default: 0).</param>
    /// <returns>List of users in the tenant.</returns>
    Task<List<User>> GetUsersAsync(int top = 100, int skip = 0);

    /// <summary>
    /// Get a user profile by ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>User object, or throws if not found.</returns>
    Task<User?> GetUserAsync(string userId);

    /// <summary>
    /// Create a new user in Microsoft Graph.
    /// </summary>
    /// <param name="user">User object to create.</param>
    /// <returns>The created User object.</returns>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Update an existing user in Microsoft Graph.
    /// </summary>
    /// <param name="user">User object with updated fields (must have <see cref="User.Id"/> set).</param>
    /// <param name="userId">User id</param>
    /// <returns>The updated User object.</returns>
    Task<User> UpdateUserAsync(User user, string userId);

    /// <summary>
    /// Delete a user from Microsoft Graph.
    /// </summary>
    /// <param name="userId">ID of the user to delete.</param>
    Task DeleteUserAsync(string userId);

    // -------------------------------------------------------------------------
    // User memberships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a user's direct group memberships (not transitive).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of direct group IDs.</returns>
    Task<List<string>> GetUserDirectGroupIdsAsync(string userId);

    /// <summary>
    /// Get a user's transitive group memberships (includes nested groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of transitive group IDs.</returns>
    Task<List<string>> GetUserTransitiveGroupIdsAsync(string userId);

    // -------------------------------------------------------------------------
    // Groups
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a group by ID.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <returns>Group object, or throws if not found.</returns>
    Task<Group?> GetGroupByIdAsync(string groupId);

    /// <summary>
    /// Query groups in Microsoft Graph, optionally filtered by display name.
    /// </summary>
    /// <param name="displayName">Display name to filter by (optional).</param>
    /// <returns>List of matching groups.</returns>
    Task<List<Group>> QueryGroupsAsync(string? displayName = null);

    /// <summary>
    /// Create a new security group in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="displayName">Display name of the group.</param>
    /// <param name="mailNickname">Mail nickname (unique alias).</param>
    /// <returns>The created Group object.</returns>
    Task<Group> CreateGroupAsync(string displayName, string mailNickname);

    /// <summary>
    /// Update an existing group in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="groupId">ID of the group to update.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="mailNickname">New mail nickname.</param>
    /// <returns>The updated Group object.</returns>
    Task<Group> UpdateGroupAsync(string groupId, string displayName, string mailNickname);

    /// <summary>
    /// Delete a group from Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="groupId">ID of the group to delete.</param>
    Task DeleteGroupAsync(string groupId);

    // -------------------------------------------------------------------------
    // Group memberships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the members of a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <returns>List of member IDs in the group.</returns>
    Task<List<string>> GetGroupMembersAsync(string groupId);

    /// <summary>
    /// Add a user to one or more groups in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="userId">ID of the user to add.</param>
    /// <param name="groupIds">List of group IDs to add the user to.</param>
    Task AddUserToGroupsAsync(string userId, List<string> groupIds);

    /// <summary>
    /// Remove a user from one or more groups in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="userId">ID of the user to remove.</param>
    /// <param name="groupIds">List of group IDs to remove the user from.</param>
    Task RemoveUserFromGroupsAsync(string userId, List<string> groupIds);

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    /// <summary>
    /// Check if the Microsoft Graph API is reachable and properly configured.
    /// </summary>
    /// <returns><c>true</c> if the API is accessible; otherwise <c>false</c>.</returns>
    Task<bool> IsAvailableAsync();
}

