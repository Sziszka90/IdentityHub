using IdentityHub.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using IdentityHub.Contracts.DTOs.Users.Responses;
using IdentityHub.Contracts.DTOs.Permissions.Responses;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for user operations: querying permissions via Microsoft Graph and managing role assignments.
/// </summary>
public class UserService : IUserService
{
    private readonly ITenantContextService _tenantContextService;
    private readonly IPermissionService _permissionService;
    private readonly IGraphService _graphService;
    private readonly IRoleService _roleService;
    private readonly ILogger<UserService> _logger;


    public UserService(
        ITenantContextService tenantContextService,
        IPermissionService permissionService,
        IGraphService graphService,
        IRoleService roleService,
        ILogger<UserService> logger)
    {
        _tenantContextService = tenantContextService ?? throw new ArgumentNullException(nameof(tenantContextService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all users with their effective permissions (tenant-scoped).
    /// </summary>
    /// <returns>List of users with their resolved groups, roles, and permissions.</returns>
    public async Task<List<UserPermissionsResponse>> GetUsersWithPermissionsAsync()
    {
        var tenantContext = _tenantContextService.GetTenantContext();

        _logger.LogInformation("Getting users for tenant: {TenantId}", tenantContext.TenantId);

        var graphUsers = await _graphService.GetUsersAsync(top: 100);
        var userPermissions = new List<UserPermissionsResponse>();

        foreach (var graphUser in graphUsers)
        {
            if (string.IsNullOrEmpty(graphUser.Id))
            {
                continue;
            }

            var groupIds = await _graphService.GetUserTransitiveGroupIdsAsync(graphUser.Id);
            var roles = await _permissionService.MapGroupsToRolesAsync(groupIds);
            var permissions = await _permissionService.ResolvePermissionsAsync(roles);

            userPermissions.Add(new UserPermissionsResponse
            {
                UserId = graphUser.Id,
                Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
                DisplayName = graphUser.DisplayName ?? "",
                TenantId = tenantContext.TenantId,
                Groups = groupIds,
                Roles = [.. roles.Select(r => r.Name)],
                Permissions = permissions
            });
        }

        return userPermissions;
    }

    /// <summary>
    /// Gets a specific user's effective permissions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user's permissions DTO, or <c>null</c> if the user was not found.</returns>
    public async Task<UserPermissionsResponse?> GetUserPermissionsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var tenantContext = _tenantContextService.GetTenantContext();

        _logger.LogInformation("Getting permissions for user {UserId} in tenant {TenantId}", userId, tenantContext.TenantId);

        var graphUser = await _graphService.GetUserAsync(userId);
        if (graphUser is null)
        {
            _logger.LogWarning("User {UserId} not found in Graph API", userId);
            throw new KeyNotFoundException($"User with ID '{userId}' was not found");
        }

        var groupIds = await _graphService.GetUserTransitiveGroupIdsAsync(userId);
        var roles = await _permissionService.MapGroupsToRolesAsync(groupIds);
        var permissions = await _permissionService.ResolvePermissionsAsync(roles);

        return new UserPermissionsResponse
        {
            UserId = graphUser.Id ?? userId,
            Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
            DisplayName = graphUser.DisplayName ?? "",
            TenantId = tenantContext.TenantId,
            Groups = groupIds,
            Roles = [.. roles.Select(r => r.Name)],
            Permissions = permissions
        };
    }

    /// <summary>
    /// Gets the detailed group → role → permission resolution chain for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The resolution chain DTO, or <c>null</c> if the user was not found.</returns>
    public async Task<PermissionResolutionChainResponse?> GetPermissionResolutionChainAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var tenantContext = _tenantContextService.GetTenantContext();

        _logger.LogInformation("Getting permission resolution chain for user {UserId} in tenant {TenantId}", userId, tenantContext.TenantId);

        var graphUser = await _graphService.GetUserAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found");

        var groupIds = await _graphService.GetUserTransitiveGroupIdsAsync(userId);
        var groupResolutions = new List<GroupResolutionResponse>();
        var allRoles = new HashSet<string>();
        var allPermissions = new HashSet<string>();

        foreach (var groupId in groupIds)
        {
            var group = await _graphService.GetGroupByIdAsync(groupId);
            var groupName = group?.DisplayName ?? groupId;

            var roles = await _permissionService.MapGroupsToRolesAsync([groupId]);
            var role = roles.FirstOrDefault();
            var permissions = role is not null
                ? await _permissionService.ResolvePermissionsAsync([role])
                : [];

            groupResolutions.Add(new GroupResolutionResponse
            {
                GroupName = groupName,
                Roles = role is not null ? [.. roles.Select(r => r.Name)] : [],
                Permissions = permissions
            });

            if (role is not null)
            {
                allRoles.Add(role.Name);
            }

            foreach (var perm in permissions)
            {
                allPermissions.Add(perm);
            }
        }

        return new PermissionResolutionChainResponse
        {
            UserId = userId,
            Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
            TenantId = tenantContext.TenantId,
            GroupResolutions = groupResolutions,
            EffectiveRoles = [.. allRoles],
            EffectivePermissions = [.. allPermissions]
        };
    }

    /// <summary>
    /// Creates a new user and assigns roles via group membership.
    /// </summary>
    /// <param name="user">User entity to create (Microsoft Graph model).</param>
    /// <param name="roleIds">List of role IDs to assign via Azure AD group membership.</param>
    /// <returns>The created <see cref="User"/> object.</returns>
    public async Task<User?> CreateUserWithRolesAsync(User user, List<string> roleIds)
    {
        var createdUser = await _graphService.CreateUserAsync(user);
        if (createdUser is null)
        {
            _logger.LogWarning("Failed to create user {UserPrincipalName}", user.UserPrincipalName);
            return null;
        }

        var groupIds = new List<Guid>();
        if (roleIds != null && roleIds.Count > 0)
        {
            foreach (var roleIdStr in roleIds)
            {
                if (!Guid.TryParse(roleIdStr, out var roleGuid))
                {
                    _logger.LogWarning("Invalid role ID format: {RoleId}", roleIdStr);
                    continue;
                }

                var mapping = await _roleService.GetGroupMappingByRoleIdAsync(roleGuid);
                if (mapping != null && mapping.GroupId != Guid.Empty)
                {
                    groupIds.Add(mapping.GroupId);
                }
                else
                {
                    _logger.LogWarning("No group mapping found for role {RoleId}", roleGuid);
                }
            }
        }

        if (groupIds.Count > 0)
        {
            await _graphService.AddUserToGroupsAsync(createdUser.Id!, [.. groupIds.Select(id => id.ToString())]);
        }

        return createdUser;
    }

    /// <summary>
    /// Assigns roles to a user via Azure AD group membership by role IDs.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to assign.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user or any role was not found.</returns>
    public async Task<UserPermissionsResponse?> AssignRolesToUserAsync(string userId, List<string> roleIds)
    {
        if (string.IsNullOrEmpty(userId) || roleIds is null || roleIds.Count == 0)
        {
            return null;
        }

        var tenantContext = _tenantContextService.GetTenantContext();

        var graphUser = await _graphService.GetUserAsync(userId);
        if (graphUser is null)
        {
            _logger.LogWarning("User {UserId} not found in Graph API", userId);
            return null;
        }

        var allPermissions = new HashSet<string>();
        var groupIds = new List<string>();
        foreach (var roleIdStr in roleIds)
        {
            if (!Guid.TryParse(roleIdStr, out var roleGuid))
            {
                _logger.LogWarning("Invalid role ID format: {RoleId}", roleIdStr);
                continue;
            }
            var mapping = await _roleService.GetGroupMappingByRoleIdAsync(roleGuid);
            if (mapping != null && mapping.GroupId != Guid.Empty)
            {
                groupIds.Add(mapping.GroupId.ToString());
            }
            else
            {
                _logger.LogWarning("No group mapping found for role {RoleId}", roleGuid);
            }
        }

        if (groupIds.Count > 0)
        {
            await _graphService.AddUserToGroupsAsync(userId, groupIds);
        }

        var roles = await _permissionService.MapGroupsToRolesAsync(groupIds);
        foreach (var role in roles)
        {
            var perms = await _permissionService.ResolvePermissionsAsync([role]);
            foreach (var perm in perms)
            {
                allPermissions.Add(perm);
            }
        }

        _logger.LogInformation("Assigned roles {Roles} to user {UserId} and updated group memberships", string.Join(", ", roleIds), userId);

        return new UserPermissionsResponse
        {
            UserId = userId,
            Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? string.Empty,
            DisplayName = graphUser.DisplayName ?? string.Empty,
            TenantId = tenantContext.TenantId,
            Groups = groupIds,
            Roles = [.. roles.Select(r => r.Name)],
            Permissions = [.. allPermissions]
        };
    }

    /// <summary>
    /// Removes roles from a user by removing them from the corresponding Azure AD groups.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="roleIds">List of role IDs to remove.</param>
    /// <returns>Updated permissions DTO for the user, or <c>null</c> if the user was not found.</returns>
    public async Task<UserPermissionsResponse?> RemoveRolesFromUserAsync(string userId, List<string> roleIds)
    {
        if (string.IsNullOrEmpty(userId) || roleIds is null || roleIds.Count == 0)
        {
            return null;
        }

        var tenantContext = _tenantContextService.GetTenantContext();

        var graphUser = await _graphService.GetUserAsync(userId);
        if (graphUser is null)
        {
            _logger.LogWarning("User {UserId} not found in Graph API", userId);
            return null;
        }

        var groupIds = new List<string>();
        foreach (var roleIdStr in roleIds)
        {
            if (!Guid.TryParse(roleIdStr, out var roleGuid))
            {
                _logger.LogWarning("Invalid role ID format: {RoleId}", roleIdStr);
                continue;
            }

            var mapping = await _roleService.GetGroupMappingByRoleIdAsync(roleGuid);
            if (mapping != null && mapping.GroupId != Guid.Empty)
            {
                groupIds.Add(mapping.GroupId.ToString());
            }
            else
            {
                _logger.LogWarning("No group mapping found for role {RoleId}", roleGuid);
            }
        }

        if (groupIds.Count > 0)
        {
            await _graphService.RemoveUserFromGroupsAsync(userId, groupIds);
        }

        _logger.LogInformation("Removed roles {Roles} from user {UserId}", string.Join(", ", roleIds), userId);

        var remainingGroupIds = await _graphService.GetUserTransitiveGroupIdsAsync(userId);
        var remainingRoles = await _permissionService.MapGroupsToRolesAsync(remainingGroupIds);
        var remainingPermissions = await _permissionService.ResolvePermissionsAsync(remainingRoles);

        return new UserPermissionsResponse
        {
            UserId = userId,
            Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? string.Empty,
            DisplayName = graphUser.DisplayName ?? string.Empty,
            TenantId = tenantContext.TenantId,
            Groups = remainingGroupIds,
            Roles = [.. remainingRoles.Select(r => r.Name)],
            Permissions = remainingPermissions
        };
    }

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    public async Task<bool> UserHasPermissionAsync(string userId, string permission)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(permission))
        {
            return false;
        }

        var userPermissionsDto = await GetUserPermissionsAsync(userId);
        if (userPermissionsDto == null)
        {
            return false;
        }

        return userPermissionsDto.Permissions.Contains(permission);
    }

    /// <summary>
    /// Checks if the user has the specified permission, supporting wildcards (e.g., "users.*", "*").
    /// </summary>
    public bool HasPermission(UserPermissionsResponse? response, string requiredPermission)
    {
        if (response?.Permissions == null)
            return false;

        // Exact match
        if (response.Permissions.Contains(requiredPermission))
            return true;

        // Wildcard match
        foreach (var perm in response.Permissions)
        {
            if (perm == "*")
            {
                return true;
            }

            if (perm.EndsWith(".*"))
            {
                var prefix = perm[..^1]; // Remove the '*'
                if (requiredPermission.StartsWith(prefix))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
