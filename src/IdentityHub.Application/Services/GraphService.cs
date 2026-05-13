using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for Microsoft Graph API operations with caching.
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
        _graphClient = graphClient ?? throw new ArgumentNullException("GraphClient is null");
        _logger = logger ?? throw new ArgumentNullException("Logger is null");
    }

    /// <summary>
    /// Create a new user in Microsoft Graph
    /// </summary>
    /// <param name="user">User object to create</param>
    /// <returns>The created User object</returns>
    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating user {UserPrincipalName} in Graph API", user.UserPrincipalName);
            var createdUser = await _graphClient.Users.PostAsync(user);
            return createdUser!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserPrincipalName} in Graph API", user.UserPrincipalName);
            throw;
        }
    }

    /// <summary>
    /// Update an existing user in Microsoft Graph
    /// </summary>
    /// <param name="user">User object with updated fields</param>
    /// <returns>The updated User object</returns>
    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Updating user {UserId} in Graph API", user.Id);
            var updatedUser = await _graphClient.Users[user.Id].PatchAsync(user);
            // Microsoft Graph PATCH returns 204 No Content, so fetch the updated user
            var fetchedUser = await _graphClient.Users[user.Id].GetAsync();
            return fetchedUser!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} in Graph API", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete a user from Microsoft Graph
    /// </summary>
    /// <param name="userId">ID of the user to delete</param>
    public async Task DeleteUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId} from Graph API", userId);
            await _graphClient.Users[userId].DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} from Graph API", userId);
            throw;
        }
    }

    /// <summary>
    /// Get user profile by ID (with caching)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    /// <remarks>
    /// SECURITY WARNING: Caching user profile data can result in stale information if the user is updated or removed in Azure AD.
    /// For critical authorization decisions, prefer real-time checks or use very short cache durations.
    /// </remarks>
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
    /// Get user's group memberships
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of group IDs the user belongs to</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<List<string>> GetUserGroupsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching groups for user {UserId} from Graph API", userId);

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
            _logger.LogError(ex, "Error fetching groups for user {UserId} from Graph API", userId);
            throw;
        }
    }

    /// <summary>
    /// Get user's transitive group memberships (includes nested groups, with caching)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of group IDs including nested group memberships</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<List<string>> GetUserTransitiveGroupsAsync(string userId)
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

    /// <summary>
    /// Get group by ID (with caching)
    /// </summary>
    /// <param name="groupId">The unique identifier of the group</param>
    /// <returns>Group object if found, null if group doesn't exist</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<Group?> GetGroupAsync(string groupId)
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
    /// Get all users in tenant (paginated)
    /// </summary>
    /// <param name="top">Maximum number of users to return (default: 100)</param>
    /// <param name="skip">Number of users to skip for pagination (default: 0)</param>
    /// <returns>List of users in the tenant</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<List<User>> GetUsersAsync(int top = 100, int skip = 0)
    {
        try
        {
            _logger.LogInformation("Fetching users from Graph API (top: {Top}, skip: {Skip})", top, skip);

            var url = $"https://graph.microsoft.com/v1.0/users?$top={top}&$skip={skip}&$select=id,displayName,mail,userPrincipalName";

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
    /// Get group members (with caching)
    /// </summary>
    /// <param name="groupId">The unique identifier of the group</param>
    /// <returns>List of member IDs in the group</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
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
    /// Check if Graph API is available
    /// </summary>
    /// <returns>True if Graph API is configured and accessible, false otherwise</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await _graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Top = 1;
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph API availability check failed");
            return false;
        }
    }

    /// <summary>
    /// Create a new group in Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="displayName">Display name of the group</param>
    /// <param name="mailNickname">Mail nickname (unique alias)</param>
    /// <returns>The created Group object</returns>
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
            return createdGroup!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {DisplayName} in Graph API", displayName);
            throw;
        }
    }

    /// <summary>
    /// Update an existing group in Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="groupId">ID of the group to update</param>
    /// <param name="displayName">New display name</param>
    /// <param name="mailNickname">New mail nickname</param>
    /// <returns>The updated Group object</returns>
    public async Task<Group> UpdateGroupAsync(string groupId, string displayName, string mailNickname)
    {
        try
        {
            _logger.LogInformation("Updating group {GroupId} in Graph API", groupId);
            var group = new Group
            {
                DisplayName = displayName,
                MailNickname = mailNickname
            };
            await _graphClient.Groups[groupId].PatchAsync(group);
            var updatedGroup = await _graphClient.Groups[groupId].GetAsync();
            return updatedGroup!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId} in Graph API", groupId);
            throw;
        }
    }

    /// <summary>
    /// Delete a group from Microsoft Graph (Azure AD)
    /// </summary>
    /// <param name="groupId">ID of the group to delete</param>
    public async Task DeleteGroupAsync(string groupId)
    {
        try
        {
            _logger.LogInformation("Deleting group {GroupId} from Graph API", groupId);
            await _graphClient.Groups[groupId].DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId} from Graph API", groupId);
            throw;
        }
    }

    /// <summary>
    /// Query groups in Microsoft Graph (Azure AD) by display name (optional)
    /// </summary>
    /// <param name="displayName">Display name to filter by (optional)</param>
    /// <returns>List of matching groups</returns>
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
}
