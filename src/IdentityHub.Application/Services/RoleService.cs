using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing roles and group-role mappings.
/// Delegates persistence to <see cref="IRolesRepository"/> and <see cref="IPermissionsRepository"/>.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRolesRepository _rolesRepository;
    private readonly IPermissionsRepository _permissionsRepository;
    private readonly IGraphService _graphService;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRolesRepository rolesRepository,
        IPermissionsRepository permissionsRepository,
        IGraphService graphService,
        ILogger<RoleService> logger)
    {
        _rolesRepository = rolesRepository ?? throw new ArgumentNullException(nameof(rolesRepository));
        _permissionsRepository = permissionsRepository ?? throw new ArgumentNullException(nameof(permissionsRepository));
        _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------------------------
    // User Role Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all roles assigned to a user by mapping their direct group memberships.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of <see cref="Role"/> entities assigned to the user via direct group membership.</returns>
    public async Task<List<Role>> GetDirectRolesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var groupIds = await _graphService.GetUserDirectGroupIdsAsync(userId, cancellationToken);

        if (groupIds == null || groupIds.Count == 0)
        {
            return [];
        }

        var mappings = await _rolesRepository.GetGroupRoleMappingsByGroupIdsAsync(groupIds, cancellationToken);

        var roleIds = mappings
            .Select(m => m.RoleId)
            .Distinct()
            .ToList();

        if (roleIds.Count == 0)
        {
            return [];
        }

        _logger.LogDebug("Resolved {Count} direct role(s) for user {UserId}", roleIds.Count, userId);
        var roles = await _rolesRepository.GetRolesByIdsAsync(roleIds);
        return roles;
    }

    /// <summary>
    /// Gets all roles assigned to a user by mapping their transitive group memberships (including nested groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of <see cref="Role"/> entities assigned to the user via transitive group membership.</returns>
    public async Task<List<Role>> GetTransitiveRolesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var groupIds = await _graphService.GetUserTransitiveGroupIdsAsync(userId);
        if (groupIds == null || groupIds.Count == 0)
        {
            return [];
        }

        var mappings = await _rolesRepository.GetGroupRoleMappingsByGroupIdsAsync(groupIds);
        var roleIds = mappings
            .Select(m => m.RoleId)
            .Distinct()
            .ToList();

        if (roleIds.Count == 0)
        {
            return [];
        }

        _logger.LogDebug("Resolved {Count} transitive role(s) for user {UserId}", roleIds.Count, userId);
        var roles = await _rolesRepository.GetRolesByIdsAsync(roleIds);
        return roles;
    }

    // -------------------------------------------------------------------------
    // Roles CRUD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all roles in the system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all <see cref="Role"/> entities.</returns>
    public Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => _rolesRepository.GetAllRolesAsync(ct);

    /// <summary>
    /// Gets a role by its unique name.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Role"/> or <c>null</c> if not found.</returns>
    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => _rolesRepository.GetRoleByNameAsync(name, ct);

    /// <summary>
    /// Gets a role by its unique ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Role"/> or <c>null</c> if not found.</returns>
    public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
        => _rolesRepository.GetRoleByIdAsync(roleId, ct);

    /// <summary>
    /// Creates a new role with the specified name, description, and permissions.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="description">Role description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Role"/> or <c>null</c> if a role with the same name exists.</returns>
    public async Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating role {RoleName} with {PermissionCount} permission(s)", name, permissions.Count);

        var existingRole = await _rolesRepository.GetRoleByNameAsync(name, ct);
        if (existingRole is not null)
        {
            _logger.LogWarning("Role {RoleName} already exists with ID {RoleId}", existingRole.Name, existingRole.Id);
            return existingRole;
        }

        var role = await _rolesRepository.CreateRoleAsync(new Role { Name = name, Description = description }, ct);

        if (permissions.Count > 0)
        {
            var permissionIds = await ResolvePermissionIdsAsync(permissions, ct);
            await _permissionsRepository.SetRolePermissionsAsync(role.Id, permissionIds, ct);
        }

        _logger.LogInformation("Created role {RoleName} with ID {RoleId}", role.Name, role.Id);
        return await _rolesRepository.GetRoleByNameAsync(role.Name, ct);
    }

    /// <summary>
    /// Updates an existing role's description and permissions.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Role"/> or <c>null</c> if not found.</returns>
    public async Task<Role?> UpdateRoleAsync(Guid roleId, string? description, List<string> permissions, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating role {RoleId} with {PermissionCount} permission(s)", roleId, permissions.Count);

        var role = await _rolesRepository.GetRoleByIdAsync(roleId, ct);
        if (role is null)
        {
            _logger.LogWarning("Role {RoleId} was not found for update", roleId);
            return null;
        }

        role.Description = description;
        await _rolesRepository.UpdateRoleAsync(role, ct);
        var permissionIds = await ResolvePermissionIdsAsync(permissions, ct);
        await _permissionsRepository.SetRolePermissionsAsync(role.Id, permissionIds, ct);

        _logger.LogInformation("Updated role {RoleId}", roleId);
        return await _rolesRepository.GetRoleByIdAsync(roleId, ct);
    }

    /// <summary>
    /// Deletes a role by ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting role {RoleId}", roleId);
        return _rolesRepository.DeleteRoleAsync(roleId, ct);
    }

    // -------------------------------------------------------------------------
    // Group-Role Mappings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all group-role mappings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of resolved <see cref="GroupRoleMapping"/> DTOs.</returns>
    public async Task<List<GroupRoleMapping>> GetAllGroupMappingsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching all group-role mappings");
        var mappings = await _rolesRepository.GetAllGroupRoleMappingsAsync(ct);
        var resolvedMappings = await Task.WhenAll(mappings.Select(mapping => ResolveGroupRoleMappingAsync(mapping, ct)));
        _logger.LogDebug("Resolved {MappingCount} group-role mapping(s)", resolvedMappings.Count(mapping => mapping is not null));
        return resolvedMappings.Where(mapping => mapping is not null).Select(mapping => mapping!).ToList();
    }

    /// <summary>
    /// Gets a group-role mapping by group name.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching resolved <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    public async Task<GroupRoleMapping?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
    {
        if (!Guid.TryParse(groupName, out var groupGuid))
        {
            _logger.LogWarning("Group identifier {GroupName} is not a valid GUID", groupName);
            return null;
        }

        var mapping = await _rolesRepository.GetGroupRoleMappingByGroupIdAsync(groupGuid, ct);
        if (mapping is null)
        {
            _logger.LogDebug("No group-role mapping was found for group {GroupId}", groupGuid);
            return null;
        }

        return await ResolveGroupRoleMappingAsync(mapping, ct);
    }

    /// <summary>
    /// Gets a group-role mapping by role ID.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    public Task<GroupRoleMapping?> GetGroupMappingByRoleIdAsync(Guid roleId, CancellationToken ct = default)
        => _rolesRepository.GetGroupRoleMappingByRoleIdAsync(roleId, ct);

    /// <summary>
    /// Creates a new group-role mapping.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="roleId">Role ID to map to the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="GroupRoleMapping"/> or <c>null</c> if a mapping for the group already exists.</returns>
    public async Task<GroupRoleMapping?> CreateGroupMappingAsync(Guid groupId, Guid roleId, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating group-role mapping for group {GroupId} and role {RoleId}", groupId, roleId);

        if (await _rolesRepository.GetGroupRoleMappingByGroupIdAsync(groupId, ct) is not null)
        {
            _logger.LogWarning("Group-role mapping already exists for group {GroupId}", groupId);
            return null;
        }

        var mapping = await _rolesRepository.CreateGroupRoleMappingAsync(
            new GroupRoleMapping { GroupId = groupId, RoleId = roleId }, ct);
        _logger.LogInformation("Created group-role mapping {MappingId}", mapping.Id);
        return mapping;
    }

    /// <summary>
    /// Updates an existing group-role mapping.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="roleId">New role ID to assign to the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    public async Task<GroupRoleMapping?> UpdateGroupMappingAsync(Guid id, Guid groupId, Guid roleId, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating group-role mapping {MappingId}", id);
        var mappings = await _rolesRepository.GetAllGroupRoleMappingsAsync(ct);
        var mapping = mappings.FirstOrDefault(m => m.Id == id);

        if (mapping is not null)
        {
            mapping.RoleId = roleId;
            mapping.GroupId = groupId;
            var updatedMapping = await _rolesRepository.UpdateGroupRoleMappingAsync(mapping, ct);
            _logger.LogInformation("Updated group-role mapping {MappingId} to group {GroupId} and role {RoleId}", id, groupId, roleId);
            return updatedMapping;
        }

        _logger.LogWarning("Group-role mapping {MappingId} was not found for update", id);
        return null;
    }

    /// <summary>
    /// Deletes a group-role mapping by ID.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteGroupMappingAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting group-role mapping {MappingId}", id);
        return _rolesRepository.DeleteGroupRoleMappingAsync(id, ct);
    }

    private async Task<GroupRoleMapping?> ResolveGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct)
    {
        try
        {
            var group = await _graphService.GetGroupByIdAsync(mapping.GroupId.ToString());

            return new GroupRoleMapping
            {
                Id = mapping.Id,
                Group = new Group
                {
                    Id = group?.Id ?? mapping.GroupId.ToString(),
                    DisplayName = group?.DisplayName ?? mapping.GroupId.ToString(),
                    MailNickname = group?.MailNickname,
                    Mail = group?.Mail,
                    Description = group?.Description,
                    SecurityEnabled = group?.SecurityEnabled
                },
                Role = new Role
                {
                    Id = mapping.Role?.Id ?? Guid.Empty,
                    Name = mapping.Role?.Name ?? string.Empty,
                    Description = mapping.Role?.Description,
                    CreatedAt = mapping.Role?.CreatedAt ?? default,
                    RolePermissions =
                        mapping.Role is not null ? mapping.Role.RolePermissions.ToList() : []
                },
                CreatedAt = mapping.CreatedAt
            };
        }
        catch (GraphResourceNotFoundException)
        {
            _logger.LogWarning("Graph group {GroupId} was not found while resolving role mapping {MappingId}", mapping.GroupId, mapping.Id);
            return null;
        }
    }

    private async Task<List<Guid>> ResolvePermissionIdsAsync(IEnumerable<string> permissionNames, CancellationToken ct)
    {
        var permissionIds = new List<Guid>();
        var permissionNameList = permissionNames.ToList();

        _logger.LogDebug("Resolving {PermissionCount} permission name(s) to IDs", permissionNameList.Count);

        foreach (var permissionName in permissionNameList)
        {
            var permission = await _permissionsRepository.GetPermissionByNameAsync(permissionName, ct);
            if (permission is null)
            {
                _logger.LogDebug("Permission {PermissionName} was missing and will be created", permissionName);
                permission = await _permissionsRepository.CreatePermissionAsync(new IdentityHub.Domain.Entities.Permission { Name = permissionName }, ct);
            }

            permissionIds.Add(permission.Id);
        }

        _logger.LogDebug("Resolved {PermissionCount} permission ID(s)", permissionIds.Count);
        return permissionIds;
    }
}
