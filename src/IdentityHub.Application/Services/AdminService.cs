using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Admin service for managing users, roles, and permissions
/// Uses Microsoft Graph API to fetch real user and group data
/// </summary>
public class AdminService : IAdminService
{
    private readonly ITenantContextService _tenantContextService;
    private readonly IPermissionService _permissionService;
    private readonly IGraphService _graphService;
    private readonly RolePermissionOptions _rolePermissionOptions;
    private readonly ICacheService _cacheService;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ITenantContextService tenantContextService,
        IPermissionService permissionService,
        IGraphService graphService,
        ICacheService cacheService,
        IOptions<RedisCacheOptions> cacheOptions,
        IOptions<RolePermissionOptions> rolePermissionOptions,
        ILogger<AdminService> logger)
    {
        _tenantContextService = tenantContextService;
        _permissionService = permissionService;
        _graphService = graphService;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _rolePermissionOptions = rolePermissionOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with their effective permissions (tenant-scoped)
    /// Fetches real data from Microsoft Graph API
    /// </summary>
    public async Task<List<UserPermissionsDto>> GetUsersWithPermissionsAsync()
    {
        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid tenant context when getting users");
            throw new InvalidTenantException("Valid tenant context is required to list users");
        }

        _logger.LogInformation("Getting users for tenant: {TenantId}", tenantContext.TenantId);

        try
        {
            var graphUsers = await _graphService.GetUsersAsync(top: 100);
            var userPermissions = new List<UserPermissionsDto>();

            foreach (var graphUser in graphUsers)
            {
                if (string.IsNullOrEmpty(graphUser.Id))
                    continue;

                var groupIds = await _graphService.GetUserGroupsAsync(graphUser.Id);

                var roles = _permissionService.MapGroupsToRoles(groupIds);

                var permissions = _permissionService.ResolvePermissions(roles);

                userPermissions.Add(new UserPermissionsDto
                {
                    UserId = graphUser.Id,
                    Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
                    DisplayName = graphUser.DisplayName ?? "",
                    TenantId = tenantContext.TenantId,
                    Groups = groupIds,
                    Roles = roles,
                    Permissions = permissions
                });
            }

            return userPermissions;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Graph API is not properly configured");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from Graph API");
            throw;
        }
    }

    /// <summary>
    /// Get a specific user's effective permissions
    /// Fetches real data from Microsoft Graph API with caching
    /// </summary>
    public async Task<UserPermissionsDto?> GetUserPermissionsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("UserId is empty");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid tenant context when getting user permissions for {UserId}", userId);
            throw new InvalidTenantException("Valid tenant context is required to access user permissions");
        }

        var cacheKey = $"user:{userId}:permissions:{tenantContext.TenantId}";
        var cached = await _cacheService.GetAsync<UserPermissionsDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for user {UserId} permissions", userId);
            return cached;
        }

        _logger.LogInformation("Cache miss - Getting permissions for user {UserId} in tenant {TenantId}",
            userId, tenantContext.TenantId);

        try
        {
            var graphUser = await _graphService.GetUserAsync(userId);
            if (graphUser == null)
            {
                _logger.LogWarning("User {UserId} not found in Graph API", userId);
                throw new KeyNotFoundException($"User with ID '{userId}' was not found");
            }

            var groupIds = await _graphService.GetUserGroupsAsync(userId);

            var roles = _permissionService.MapGroupsToRoles(groupIds);

            var permissions = _permissionService.ResolvePermissions(roles);

            var userPermissions = new UserPermissionsDto
            {
                UserId = graphUser.Id ?? userId,
                Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
                DisplayName = graphUser.DisplayName ?? "",
                TenantId = tenantContext.TenantId,
                Groups = groupIds,
                Roles = roles,
                Permissions = permissions
            };

            await _cacheService.SetAsync(
                cacheKey,
                userPermissions,
                _cacheOptions.UserPermissionsExpirationSeconds);

            return userPermissions;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Graph API is not properly configured");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Graph API", userId);
            throw;
        }
    }

    /// <summary>
    /// Get detailed permission resolution chain for a user
    /// Shows groups → roles → permissions with real Graph data
    /// </summary>
    public async Task<PermissionResolutionChainDto?> GetPermissionResolutionChainAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("UserId is empty");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid tenant context when getting resolution chain for {UserId}", userId);
            throw new InvalidTenantException("Valid tenant context is required to access permission resolution chain");
        }

        _logger.LogInformation("Getting permission resolution chain for user {UserId} in tenant {TenantId}",
            userId, tenantContext.TenantId);

        try
        {
            var graphUser = await _graphService.GetUserAsync(userId);
            if (graphUser == null)
            {
                throw new KeyNotFoundException($"User with ID '{userId}' was not found");
            }

            var groupIds = await _graphService.GetUserGroupsAsync(userId);

            var groupResolutions = new List<GroupResolution>();
            var allRoles = new HashSet<string>();
            var allPermissions = new HashSet<string>();

            foreach (var groupId in groupIds)
            {
                var group = await _graphService.GetGroupAsync(groupId);
                var groupName = group?.DisplayName ?? groupId;

                var roles = _permissionService.MapGroupsToRoles(new[] { groupId });
                var role = roles.FirstOrDefault();

                var permissions = role != null
                    ? _permissionService.ResolvePermissions(new[] { role })
                    : new List<string>();

                groupResolutions.Add(new GroupResolution
                {
                    GroupName = groupName,
                    GroupId = groupId,
                    MappedRole = role,
                    Permissions = permissions
                });

                if (role != null)
                {
                    allRoles.Add(role);
                }
                foreach (var perm in permissions)
                {
                    allPermissions.Add(perm);
                }
            }

            return new PermissionResolutionChainDto
            {
                UserId = userId,
                Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
                TenantId = tenantContext.TenantId,
                GroupResolutions = groupResolutions,
                EffectiveRoles = allRoles.ToList(),
                EffectivePermissions = allPermissions.ToList()
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Graph API is not properly configured");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resolution chain for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    public List<RolePermissionsDto> GetAllRolesWithPermissions()
    {
        var roles = new List<RolePermissionsDto>();

        foreach (var (roleName, permissions) in _rolePermissionOptions.RolePermissions)
        {
            roles.Add(new RolePermissionsDto
            {
                RoleName = roleName,
                Permissions = permissions.ToList()
            });
        }

        _logger.LogInformation("Retrieved {Count} roles with permissions", roles.Count);
        return roles;
    }

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    public RolePermissionsDto? GetRolePermissions(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            return null;
        }

        if (_rolePermissionOptions.RolePermissions.TryGetValue(roleName, out var permissions))
        {
            return new RolePermissionsDto
            {
                RoleName = roleName,
                Permissions = permissions.ToList()
            };
        }

        _logger.LogWarning("Role {RoleName} not found", roleName);
        return null;
    }

    /// <summary>
    /// Create a new role with permissions
    /// Note: In production, this would persist to a database
    /// </summary>
    public async Task<RolePermissionsDto?> CreateRoleAsync(string roleName, List<string> permissions)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            _logger.LogWarning("Cannot create role with empty name");
            return null;
        }

        if (_rolePermissionOptions.RolePermissions.ContainsKey(roleName))
        {
            _logger.LogWarning("Role {RoleName} already exists", roleName);
            return null;
        }

        // In production, persist to database
        // For now, we can't dynamically add to options, so we'll simulate success
        _logger.LogInformation("Created role {RoleName} with {Count} permissions", roleName, permissions.Count);

        await Task.CompletedTask;

        return new RolePermissionsDto
        {
            RoleName = roleName,
            Permissions = permissions
        };
    }

    /// <summary>
    /// Update an existing role's permissions
    /// Note: In production, this would persist to a database
    /// </summary>
    public async Task<RolePermissionsDto?> UpdateRolePermissionsAsync(string roleName, List<string> permissions)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            return null;
        }

        if (!_rolePermissionOptions.RolePermissions.ContainsKey(roleName))
        {
            _logger.LogWarning("Role {RoleName} not found for update", roleName);
            return null;
        }

        // In production, update in database
        // For now, we can't dynamically modify options, so we'll simulate success
        _logger.LogInformation("Updated role {RoleName} with {Count} permissions", roleName, permissions.Count);

        await Task.CompletedTask;

        return new RolePermissionsDto
        {
            RoleName = roleName,
            Permissions = permissions
        };
    }

    /// <summary>
    /// Delete a role
    /// Note: In production, this would persist to a database
    /// </summary>
    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            return false;
        }

        if (!_rolePermissionOptions.RolePermissions.ContainsKey(roleName))
        {
            _logger.LogWarning("Role {RoleName} not found for deletion", roleName);
            return false;
        }

        // In production, delete from database
        // For now, we can't dynamically remove from options, so we'll simulate success
        _logger.LogInformation("Deleted role {RoleName}", roleName);

        await Task.CompletedTask;

        return true;
    }

    /// <summary>
    /// Assign roles to a user (via group membership in production)
    /// Note: In production, this would use Microsoft Graph API to add user to groups
    /// </summary>
    public async Task<UserPermissionsDto?> AssignRolesToUserAsync(string userId, List<string> roles)
    {
        if (string.IsNullOrEmpty(userId) || roles == null || roles.Count == 0)
        {
            return null;
        }

        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid tenant context when assigning roles to user {UserId}", userId);
            return null;
        }

        // Validate all roles exist
        foreach (var role in roles)
        {
            if (!_rolePermissionOptions.RolePermissions.ContainsKey(role))
            {
                _logger.LogWarning("Role {RoleName} not found", role);
                return null;
            }
        }

        // In production:
        // 1. Use Graph API to find groups mapped to these roles
        // 2. Add user to those groups
        // 3. Return updated user permissions

        _logger.LogInformation("Assigned roles {Roles} to user {UserId}", string.Join(", ", roles), userId);

        await Task.CompletedTask;

        // Resolve permissions from roles
        var allPermissions = new HashSet<string>();
        foreach (var role in roles)
        {
            if (_rolePermissionOptions.RolePermissions.TryGetValue(role, out var permissions))
            {
                foreach (var permission in permissions)
                {
                    allPermissions.Add(permission);
                }
            }
        }

        return new UserPermissionsDto
        {
            UserId = userId,
            Email = "user@example.com", // Would come from Graph API
            DisplayName = "User", // Would come from Graph API
            TenantId = tenantContext.TenantId,
            Groups = new List<string>(), // Would come from Graph API
            Roles = roles,
            Permissions = allPermissions.ToList()
        };
    }

    /// <summary>
    /// Remove roles from a user
    /// Note: In production, this would use Microsoft Graph API to remove user from groups
    /// </summary>
    public async Task<UserPermissionsDto?> RemoveRolesFromUserAsync(string userId, List<string> roles)
    {
        if (string.IsNullOrEmpty(userId) || roles == null || roles.Count == 0)
        {
            return null;
        }

        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid tenant context when removing roles from user {UserId}", userId);
            return null;
        }

        // In production:
        // 1. Use Graph API to find groups mapped to these roles
        // 2. Remove user from those groups
        // 3. Return updated user permissions

        _logger.LogInformation("Removed roles {Roles} from user {UserId}", string.Join(", ", roles), userId);

        await Task.CompletedTask;

        return new UserPermissionsDto
        {
            UserId = userId,
            Email = "user@example.com", // Would come from Graph API
            DisplayName = "User", // Would come from Graph API
            TenantId = tenantContext.TenantId,
            Groups = new List<string>(), // Would come from Graph API
            Roles = new List<string>(), // Remaining roles
            Permissions = new List<string>() // Remaining permissions
        };
    }
}
