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
    private readonly ILogger<AdminService> _logger;
    private readonly IRolesRepository _rolesRepository;

    public AdminService(
        ITenantContextService tenantContextService,
        IPermissionService permissionService,
        IGraphService graphService,
        IOptions<RolePermissionOptions> rolePermissionOptions,
        ILogger<AdminService> logger,
        IRolesRepository rolesRepository)
    {
        _tenantContextService = tenantContextService;
        _permissionService = permissionService;
        _graphService = graphService;
        _rolePermissionOptions = rolePermissionOptions.Value;
        _logger = logger;
        _rolesRepository = rolesRepository;
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
                {
                    continue;
                }

                var groupIds = await _graphService.GetUserGroupsAsync(graphUser.Id);
                var roles = await _permissionService.MapGroupsToRoles(groupIds);
                var permissions = await _permissionService.ResolvePermissions(roles);

                var userPermission = new UserPermissionsDto
                {
                    UserId = graphUser.Id,
                    Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "",
                    DisplayName = graphUser.DisplayName ?? "",
                    TenantId = tenantContext.TenantId,
                    Groups = groupIds,
                    Roles = roles,
                    Permissions = permissions
                };

                userPermissions.Add(userPermission);
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

        _logger.LogInformation("Getting permissions for user {UserId} in tenant {TenantId}",
            userId, tenantContext.TenantId);

        try
        {
            var graphUser = await _graphService.GetUserAsync(userId);
            if (graphUser is null)
            {
                _logger.LogWarning("User {UserId} not found in Graph API", userId);
                throw new KeyNotFoundException($"User with ID '{userId}' was not found");
            }

            var groupIds = await _graphService.GetUserGroupsAsync(userId);

            var roles = await _permissionService.MapGroupsToRoles(groupIds);

            var permissions = await _permissionService.ResolvePermissions(roles);

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
            var graphUser = await _graphService.GetUserAsync(userId) ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found");
            var groupIds = await _graphService.GetUserGroupsAsync(userId);

            var groupResolutions = new List<GroupResolution>();
            var allRoles = new HashSet<string>();
            var allPermissions = new HashSet<string>();

            foreach (var groupId in groupIds)
            {
                var group = await _graphService.GetGroupAsync(groupId);
                var groupName = group?.DisplayName ?? groupId;

                var roles = await _permissionService.MapGroupsToRoles([groupId]);
                var role = roles.FirstOrDefault();

                var permissions = role != null
                    ? await _permissionService.ResolvePermissions([role])
                    : [];

                groupResolutions.Add(new GroupResolution
                {
                    GroupName = groupName,
                    GroupId = groupId,
                    MappedRole = role,
                    Permissions = permissions
                });

                if (role is not null)
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
                EffectiveRoles = [.. allRoles],
                EffectivePermissions = [.. allPermissions]
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
    public async Task<List<RolePermissionsDto>> GetAllRolesWithPermissionsAsync()
    {
        // Fetch all roles from the DB (or remote client) via IPermissionService
        var roles = new List<RolePermissionsDto>();
        try
        {
            // Get all role names from the DB/config
            var allRolePermissions = await _permissionService.ResolvePermissions(_rolePermissionOptions.RolePermissions.Keys);
            foreach (var roleName in _rolePermissionOptions.RolePermissions.Keys)
            {
                var permissions = await _permissionService.ResolvePermissions([roleName]);
                roles.Add(new RolePermissionsDto
                {
                    RoleName = roleName,
                    Permissions = permissions
                });
            }
            _logger.LogInformation("Retrieved {Count} roles with permissions (from DB or remote)", roles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch roles with permissions from DB or remote, falling back to options");
            // Fallback to options
            foreach (var (roleName, permissions) in _rolePermissionOptions.RolePermissions)
            {
                roles.Add(new RolePermissionsDto
                {
                    RoleName = roleName,
                    Permissions = [.. permissions]
                });
            }
        }
        return roles;
    }

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            return null;
        }

        try
        {
            var permissions = await _permissionService.ResolvePermissions([roleName]);
            if (permissions != null && permissions.Count > 0)
            {
                return new RolePermissionsDto
                {
                    RoleName = roleName,
                    Permissions = permissions
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch permissions for role {RoleName} from DB or remote, falling back to options", roleName);
        }

        // Fallback to options
        if (_rolePermissionOptions.RolePermissions.TryGetValue(roleName, out var optionPerms))
        {
            return new RolePermissionsDto
            {
                RoleName = roleName,
                Permissions = [.. optionPerms]
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

        // Check config first
        if (_rolePermissionOptions.RolePermissions.ContainsKey(roleName))
        {
            _logger.LogWarning("Role {RoleName} already exists in config", roleName);
            return null;
        }

        // Check DB
        var existingRole = await _rolesRepository.GetRoleByNameAsync(roleName);
        if (existingRole != null)
        {
            _logger.LogWarning("Role {RoleName} already exists in DB", roleName);
            return null;
        }

        // Create role entity
        var newRole = new IdentityHub.Domain.Entities.Role
        {
            Name = roleName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Permissions will be added below
        };

        // Map permissions to RolePermission entities
        foreach (var perm in permissions)
        {
            newRole.RolePermissions.Add(new IdentityHub.Domain.Entities.RolePermission
            {
                Permission = new IdentityHub.Domain.Entities.Permission { Name = perm }
            });
        }

        var createdRole = await _rolesRepository.CreateRoleAsync(newRole);

        _logger.LogInformation("Created role {RoleName} with {Count} permissions in DB", roleName, permissions.Count);

        return new RolePermissionsDto
        {
            RoleName = createdRole.Name,
            Permissions = createdRole.RolePermissions.Select(rp => rp.Permission.Name).ToList()
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

        // Check DB for role
        var existingRole = await _rolesRepository.GetRoleByNameAsync(roleName);
        if (existingRole == null)
        {
            _logger.LogWarning("Role {RoleName} not found in DB for update", roleName);
            return null;
        }

        // Update permissions: clear and add new
        existingRole.RolePermissions.Clear();
        foreach (var perm in permissions)
        {
            existingRole.RolePermissions.Add(new IdentityHub.Domain.Entities.RolePermission
            {
                Permission = new IdentityHub.Domain.Entities.Permission { Name = perm }
            });
        }
        existingRole.UpdatedAt = DateTime.UtcNow;

        var updatedRole = await _rolesRepository.UpdateRoleAsync(existingRole);

        _logger.LogInformation("Updated role {RoleName} with {Count} permissions in DB", roleName, permissions.Count);

        return new RolePermissionsDto
        {
            RoleName = updatedRole.Name,
            Permissions = updatedRole.RolePermissions.Select(rp => rp.Permission.Name).ToList()
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

        // Check DB for role
        var existingRole = await _rolesRepository.GetRoleByNameAsync(roleName);
        if (existingRole == null)
        {
            _logger.LogWarning("Role {RoleName} not found in DB for deletion", roleName);
            return false;
        }

        var deleted = await _rolesRepository.DeleteRoleAsync(existingRole.Id);
        if (deleted)
        {
            _logger.LogInformation("Deleted role {RoleName} from DB", roleName);
        }
        else
        {
            _logger.LogWarning("Failed to delete role {RoleName} from DB", roleName);
        }
        return deleted;
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

        // Fetch user from Graph API
        var graphUser = await _graphService.GetUserAsync(userId);
        if (graphUser is null)
        {
            _logger.LogWarning("User {UserId} not found in Graph API", userId);
            return null;
        }

        // Check all roles exist in DB and aggregate permissions in a single loop
        var allPermissions = new HashSet<string>();
        foreach (var role in roles)
        {
            var dbRole = await _rolesRepository.GetRoleByNameAsync(role);
            if (dbRole == null)
            {
                _logger.LogWarning("Role {RoleName} not found in DB", role);
                return null;
            }
            foreach (var rp in dbRole.RolePermissions)
            {
                allPermissions.Add(rp.Permission.Name);
            }
        }

        // In production: Use Graph API to add user to groups mapped to these roles
        _logger.LogInformation("Assigned roles {Roles} to user {UserId} (simulated)", string.Join(", ", roles), userId);

        return new UserPermissionsDto
        {
            UserId = userId,
            Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? string.Empty,
            DisplayName = graphUser.DisplayName ?? string.Empty,
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
