using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for Microsoft Graph API operations.
///
/// <para><b>SECURITY WARNING:</b> Caching user or group membership data can result in stale authorization information if changes occur in Azure AD (e.g., user removed from group, group deleted, etc.).
/// For critical authorization decisions, prefer real-time checks or use very short cache durations (minutes, not hours/days).
/// Always document the risk that cached data may not reflect immediate changes in Azure.</para>
/// </summary>
public class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphService> _logger;

    public GraphService(
        GraphServiceClient graphClient,
        ILogger<GraphService> logger)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------------------------
    // Users
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get all users in the tenant (paginated).
    /// </summary>
    /// <param name="top">Maximum number of users to return (default: 100).</param>
    /// <param name="skip">Number of users to skip for pagination (default: 0).</param>
    /// <returns>List of users in the tenant.</returns>
    public async Task<List<User>> GetUsersAsync(int top = 100, int skip = 0)
    {
        try
        {
            _logger.LogInformation("Fetching users from Graph API (top: {Top}, skip: {Skip})", top, skip);

            var url = $"https://graph.microsoft.com/v1.0/users?$top={top}&$select=id,displayName,mail,userPrincipalName";
            var users = await _graphClient.Users.WithUrl(url).GetAsync();

            return users?.Value?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from Graph API");
            throw;
        }
    }

    /// <summary>
    /// Get a user profile by ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>User object, or throws if not found.</returns>
    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching user {UserId} from Graph API", userId);

            var user = await _graphClient.Users[userId].GetAsync();

            return user;
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("User {UserId} not found in Graph API", userId);
            throw GraphResourceNotFoundException.ForUser(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Graph API", userId);
            throw;
        }
    }

    /// <summary>
    /// Create a new user in Microsoft Graph.
    /// </summary>
    /// <param name="user">User object to create.</param>
    /// <returns>The created User object.</returns>
    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating user {UserPrincipalName} in Graph API", user.UserPrincipalName);

            var createdUser = await _graphClient.Users.PostAsync(user);
            if (createdUser == null)
            {
                _logger.LogError("Graph API returned null when creating user {UserPrincipalName}", user.UserPrincipalName);
                throw new InvalidOperationException("Failed to create user in Microsoft Graph.");
            }

            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserPrincipalName} in Graph API", user.UserPrincipalName);
            throw;
        }
    }

    /// <summary>
    /// Update an existing user in Microsoft Graph.
    /// </summary>
    /// <param name="user">User object with updated fields (must have <see cref="User.Id"/> set).</param>
    /// <returns>The updated User object.</returns>
    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Updating user {UserId} in Graph API", user.Id);

            var existingUser = await _graphClient.Users[user.Id].GetAsync();
            if (existingUser == null)
            {
                _logger.LogWarning("User {UserId} does not exist in Graph API", user.Id);
                throw new KeyNotFoundException($"User with ID '{user.Id}' does not exist in Microsoft Graph.");
            }

            _ = await _graphClient.Users[user.Id].PatchAsync(user);

            var fetchedUser = await _graphClient.Users[user.Id].GetAsync();
            if (fetchedUser == null)
            {
                _logger.LogError("Graph API returned null after updating user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to fetch updated user from Microsoft Graph.");
            }

            return fetchedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} in Graph API", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete a user from Microsoft Graph.
    /// </summary>
    /// <param name="userId">ID of the user to delete.</param>
    public async Task DeleteUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId} from Graph API", userId);
            await _graphClient.Users[userId].DeleteAsync();

            var user = await _graphClient.Users[userId].GetAsync();
            if (user is not null)
            {
                _logger.LogWarning("User {UserId} still exists after deletion attempt.", userId);
                throw new InvalidOperationException($"User {userId} was not deleted from Graph API.");
            }
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("User {UserId} successfully deleted from Graph API.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting or validating deletion of user {UserId} from Graph API", userId);
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // User memberships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a user's direct group memberships (not transitive).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of direct group IDs.</returns>
    public async Task<List<string>> GetUserDirectGroupIdsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching direct groups for user {UserId} from Graph API", userId);

            var groups = new List<string>();
            var memberOf = await _graphClient.Users[userId].MemberOf.GetAsync();

            if (memberOf?.Value is not null)
            {
                foreach (var directoryObject in memberOf.Value)
                {
                    if (directoryObject is Group group && group.Id is not null)
                    {
                        groups.Add(group.Id);
                    }
                }
            }

            return groups;
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("User {UserId} not found in Graph API", userId);
            throw GraphResourceNotFoundException.ForUser(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching direct groups for user {UserId} from Graph API", userId);
            throw;
        }
    }

    /// <summary>
    /// Get a user's transitive group memberships (includes nested groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of transitive group IDs.</returns>
    public async Task<List<string>> GetUserTransitiveGroupIdsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching transitive groups for user {UserId} from Graph API", userId);

            var groups = new List<string>();
            var memberOf = await _graphClient.Users[userId].TransitiveMemberOf.GetAsync();

            if (memberOf?.Value is not null)
            {
                foreach (var directoryObject in memberOf.Value)
                {
                    if (directoryObject is Group group && group.Id is not null)
                    {
                        groups.Add(group.Id);
                    }
                }
            }

            return groups;
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("User {UserId} not found in Graph API", userId);
            throw GraphResourceNotFoundException.ForUser(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transitive groups for user {UserId} from Graph API", userId);
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Groups
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a group by ID.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <returns>Group object, or throws if not found.</returns>
    public async Task<Group?> GetGroupByIdAsync(string groupId)
    {
        try
        {
            _logger.LogInformation("Fetching group {GroupId} from Graph API", groupId);

            var group = await _graphClient.Groups[groupId].GetAsync();

            return group;
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("Group {GroupId} not found in Graph API", groupId);
            throw GraphResourceNotFoundException.ForGroup(groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching group {GroupId} from Graph API", groupId);
            throw;
        }
    }

    /// <summary>
    /// Query groups in Microsoft Graph, optionally filtered by display name.
    /// </summary>
    /// <param name="displayName">Display name to filter by (optional).</param>
    /// <returns>List of matching groups.</returns>
    public async Task<List<Group>> QueryGroupsAsync(string? displayName = null)
    {
        try
        {
            _logger.LogInformation("Querying groups from Graph API. Filter: {DisplayName}", displayName);

            var query = _graphClient.Groups;
            if (!string.IsNullOrEmpty(displayName))
            {
                query = query.WithUrl($"https://graph.microsoft.com/v1.0/groups?$filter=displayName eq '{displayName}'");
            }

            var result = await query.GetAsync();
            return result?.Value?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying groups from Graph API");
            throw;
        }
    }

    /// <summary>
    /// Create a new security group in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="displayName">Display name of the group.</param>
    /// <param name="mailNickname">Mail nickname (unique alias).</param>
    /// <returns>The created Group object.</returns>
    public async Task<Group> CreateGroupAsync(string displayName, string mailNickname)
    {
        try
        {
            _logger.LogInformation("Creating group {DisplayName} in Graph API", displayName);

            var group = new Group
            {
                DisplayName = displayName,
                MailEnabled = false,
                MailNickname = mailNickname,
                SecurityEnabled = true,
                GroupTypes = []
            };

            var createdGroup = await _graphClient.Groups.PostAsync(group);
            if (createdGroup == null)
            {
                _logger.LogError("Graph API returned null when creating group {DisplayName}", displayName);
                throw new InvalidOperationException("Failed to create group in Microsoft Graph.");
            }

            return createdGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {DisplayName} in Graph API", displayName);
            throw;
        }
    }

    /// <summary>
    /// Update an existing group in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="groupId">ID of the group to update.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="mailNickname">New mail nickname.</param>
    /// <returns>The updated Group object.</returns>
    public async Task<Group> UpdateGroupAsync(string groupId, string displayName, string mailNickname)
    {
        try
        {
            _logger.LogInformation("Updating group {GroupId} in Graph API", groupId);

            var existingGroup = await _graphClient.Groups[groupId].GetAsync();
            if (existingGroup == null)
            {
                _logger.LogWarning("Group {GroupId} does not exist in Graph API", groupId);
                throw new KeyNotFoundException($"Group with ID '{groupId}' does not exist in Microsoft Graph.");
            }

            var group = new Group
            {
                DisplayName = displayName,
                MailNickname = mailNickname
            };

            await _graphClient.Groups[groupId].PatchAsync(group);

            var updatedGroup = await _graphClient.Groups[groupId].GetAsync();
            if (updatedGroup == null)
            {
                _logger.LogError("Graph API returned null when updating group {GroupId}", groupId);
                throw new InvalidOperationException("Failed to update group in Microsoft Graph.");
            }

            return updatedGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId} in Graph API", groupId);
            throw;
        }
    }

    /// <summary>
    /// Delete a group from Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="groupId">ID of the group to delete.</param>
    public async Task DeleteGroupAsync(string groupId)
    {
        try
        {
            _logger.LogInformation("Deleting group {GroupId} from Graph API", groupId);
            await _graphClient.Groups[groupId].DeleteAsync();

            var group = await _graphClient.Groups[groupId].GetAsync();
            if (group != null)
            {
                _logger.LogWarning("Group {GroupId} still exists after deletion attempt.", groupId);
                throw new InvalidOperationException($"Group {groupId} was not deleted from Graph API.");
            }
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("Group {GroupId} successfully deleted from Graph API.", groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting or validating deletion of group {GroupId} from Graph API", groupId);
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Group memberships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the members of a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <returns>List of member IDs in the group.</returns>
    public async Task<List<string>> GetGroupMembersAsync(string groupId)
    {
        try
        {
            _logger.LogInformation("Fetching members for group {GroupId} from Graph API", groupId);

            var members = new List<string>();
            var groupMembers = await _graphClient.Groups[groupId].Members.GetAsync();

            if (groupMembers?.Value is not null)
            {
                foreach (var directoryObject in groupMembers.Value)
                {
                    if (directoryObject.Id is not null)
                    {
                        members.Add(directoryObject.Id);
                    }
                }
            }

            return members;
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogInformation("Group {GroupId} not found in Graph API", groupId);
            throw GraphResourceNotFoundException.ForGroup(groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching members for group {GroupId} from Graph API", groupId);
            throw;
        }
    }

    /// <summary>
    /// Add a user to one or more groups in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="userId">ID of the user to add.</param>
    /// <param name="groupIds">List of group IDs to add the user to.</param>
    public async Task AddUserToGroupsAsync(string userId, List<string> groupIds)
    {
        foreach (var groupId in groupIds)
        {
            try
            {
                _logger.LogInformation("Adding user {UserId} to group {GroupId} in Graph API", userId, groupId);

                var reference = new ReferenceCreate
                {
                    OdataId = $"https://graph.microsoft.com/v1.0/users/{userId}"
                };

                await _graphClient.Groups[groupId.ToString()].Members.Ref.PostAsync(reference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to group {GroupId} in Graph API", userId, groupId);
                throw;
            }
        }
    }

    /// <summary>
    /// Remove a user from one or more groups in Microsoft Graph (Azure AD).
    /// </summary>
    /// <param name="userId">ID of the user to remove.</param>
    /// <param name="groupIds">List of group IDs to remove the user from.</param>
    public async Task RemoveUserFromGroupsAsync(string userId, List<string> groupIds)
    {
        foreach (var groupId in groupIds)
        {
            try
            {
                _logger.LogInformation("Removing user {UserId} from group {GroupId} in Graph API", userId, groupId);
                await _graphClient.Groups[groupId].Members[userId].Ref.DeleteAsync();
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                _logger.LogInformation("User {UserId} or group {GroupId} not found; skipping removal", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from group {GroupId} in Graph API", userId, groupId);
                throw;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    /// <summary>
    /// Check if the Microsoft Graph API is reachable and properly configured.
    /// </summary>
    /// <returns><c>true</c> if the API is accessible; otherwise <c>false</c>.</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await _graphClient.Users.GetAsync(config => config.QueryParameters.Top = 1);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph API availability check failed");
            return false;
        }
    }
}
