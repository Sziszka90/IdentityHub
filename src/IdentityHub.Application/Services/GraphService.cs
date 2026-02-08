using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Exceptions;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for Microsoft Graph API operations with caching
/// </summary>
public class GraphService : IGraphService
{
    private readonly GraphServiceClient? _graphClient;
    private readonly ICacheService _cacheService;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly ILogger<GraphService> _logger;
    private readonly bool _isAvailable;

    public GraphService(
        GraphServiceClient? graphClient,
        ICacheService cacheService,
        IOptions<RedisCacheOptions> cacheOptions,
        ILogger<GraphService> logger)
    {
        _graphClient = graphClient;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _isAvailable = graphClient != null;

        if (!_isAvailable)
        {
            _logger.LogWarning("Graph API client is not configured");
        }
    }

    /// <summary>
    /// Get user profile by ID (with caching)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<User?> GetUserAsync(string userId)
    {
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        var cacheKey = $"graph:user:{userId}";
        var cached = await _cacheService.GetAsync<User>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for user {UserId}", userId);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching user {UserId} from Graph API", userId);
            var user = await _graphClient.Users[userId].GetAsync();

            if (user is not null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    user,
                    _cacheOptions.GraphDataExpirationSeconds);
            }

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
    /// Get user's group memberships (with caching)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of group IDs the user belongs to</returns>
    /// <exception cref="InvalidOperationException">Graph API is not configured</exception>
    public async Task<List<string>> GetUserGroupsAsync(string userId)
    {
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        var cacheKey = $"graph:user:{userId}:groups";
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for user {UserId} groups", userId);
            return cached;
        }

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

            await _cacheService.SetAsync(
                cacheKey,
                groups,
                _cacheOptions.GraphDataExpirationSeconds);

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
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        var cacheKey = $"graph:user:{userId}:transitive-groups";
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for user {UserId} transitive groups", userId);
            return cached;
        }

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

            await _cacheService.SetAsync(
                cacheKey,
                groups,
                _cacheOptions.GraphDataExpirationSeconds);

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
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        var cacheKey = $"graph:group:{groupId}";
        var cached = await _cacheService.GetAsync<Group>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for group {GroupId}", groupId);
            return cached;
        }

        try
        {
            _logger.LogInformation("Fetching group {GroupId} from Graph API", groupId);
            var group = await _graphClient.Groups[groupId].GetAsync();

            if (group is not null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    group,
                    _cacheOptions.RolePermissionsExpirationSeconds);
            }

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
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        try
        {
            _logger.LogInformation("Fetching users from Graph API (top: {Top})", top);

            var users = await _graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Top = top;
                config.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName" };
            });

            return users?.Value?.ToList() ?? new List<User>();
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
        if (!_isAvailable || _graphClient is null)
        {
            throw new InvalidOperationException("Graph API is not configured. Check EntraId settings.");
        }

        var cacheKey = $"graph:group:{groupId}:members";
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for group {GroupId} members", groupId);
            return cached;
        }

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

            await _cacheService.SetAsync(
                cacheKey,
                members,
                _cacheOptions.GraphDataExpirationSeconds);

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
        if (!_isAvailable || _graphClient is null)
        {
            return false;
        }

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
}
