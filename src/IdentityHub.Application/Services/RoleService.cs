using IdentityHub.Application.Interfaces;
using IdentityHub.Contracts.DTOs.Groups.Responses;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using IdentityHub.Contracts.DTOs.Roles.Responses;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;

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
    public async Task<List<Role>> GetDirectRolesForUserAsync(string userId)
    {
        var groupIds = await _graphService.GetUserDirectGroupIdsAsync(userId);
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

        _logger.LogDebug("Resolved {Count} direct role(s) for user {UserId}", roleIds.Count, userId);
        var roles = await _rolesRepository.GetRolesByIdsAsync(roleIds);
        return roles;
    }

    /// <summary>
    /// Gets all roles assigned to a user by mapping their transitive group memberships (including nested groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>List of <see cref="Role"/> entities assigned to the user via transitive group membership.</returns>
    public async Task<List<Role>> GetTransitiveRolesForUserAsync(string userId)
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
    /// Creates a new role with the specified name, description, and permissions.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="description">Role description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Role"/> or <c>null</c> if a role with the same name exists.</returns>
    public async Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        var existingRole = await _rolesRepository.GetRoleByNameAsync(name, ct);
        if (existingRole is not null)
        {
            return existingRole;
        }

        var role = await _rolesRepository.CreateRoleAsync(new Role { Name = name, Description = description }, ct);

        if (permissions.Count > 0)
        {
            await _permissionsRepository.SetRolePermissionsAsync(role.Name, permissions, ct);
        }

        return await _rolesRepository.GetRoleByNameAsync(role.Name, ct);
    }

    /// <summary>
    /// Updates an existing role's description and permissions.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="permissions">List of permissions to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Role"/> or <c>null</c> if not found.</returns>
    public async Task<Role?> UpdateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _rolesRepository.GetRoleByNameAsync(name, ct);
        if (role is null)
        {
            return null;
        }

        role.Description = description;
        await _rolesRepository.UpdateRoleAsync(role, ct);
        await _permissionsRepository.SetRolePermissionsAsync(name, permissions, ct);

        return await _rolesRepository.GetRoleByNameAsync(name, ct);
    }

    /// <summary>
    /// Deletes a role by name.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public async Task<bool> DeleteRoleAsync(string name, CancellationToken ct = default)
    {
        var role = await _rolesRepository.GetRoleByNameAsync(name, ct);
        if (role is null)
        {
            return false;
        }

        return await _rolesRepository.DeleteRoleAsync(role.Id, ct);
    }

    // -------------------------------------------------------------------------
    // Group-Role Mappings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets all group-role mappings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of resolved <see cref="GroupRoleMappingResponse"/> DTOs.</returns>
    public async Task<List<GroupRoleMappingResponse>> GetAllGroupMappingsAsync(CancellationToken ct = default)
    {
        var mappings = await _rolesRepository.GetAllGroupRoleMappingsAsync(ct);
        var resolvedMappings = await Task.WhenAll(mappings.Select(mapping => ResolveGroupRoleMappingAsync(mapping, ct)));
        return resolvedMappings.Where(mapping => mapping is not null).Select(mapping => mapping!).ToList();
    }

    /// <summary>
    /// Gets a group-role mapping by group name.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching resolved <see cref="GroupRoleMappingResponse"/> or <c>null</c> if not found.</returns>
    public async Task<GroupRoleMappingResponse?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
    {
        if (!Guid.TryParse(groupName, out var groupGuid))
        {
            return null;
        }

        var mapping = await _rolesRepository.GetGroupRoleMappingByGroupIdAsync(groupGuid, ct);
        if (mapping is null)
        {
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
        if (await _rolesRepository.GetGroupRoleMappingByGroupIdAsync(groupId, ct) is not null)
        {
            return null;
        }

        return await _rolesRepository.CreateGroupRoleMappingAsync(
            new GroupRoleMapping { GroupId = groupId, RoleId = roleId }, ct);
    }

    /// <summary>
    /// Updates an existing group-role mapping.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="roleId">New role ID to assign to the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="GroupRoleMapping"/> or <c>null</c> if not found.</returns>
    public async Task<GroupRoleMapping?> UpdateGroupMappingAsync(Guid id, Guid roleId, CancellationToken ct = default)
    {
        var mappings = await _rolesRepository.GetAllGroupRoleMappingsAsync(ct);
        var mapping = mappings.FirstOrDefault(m => m.Id == id);
        if (mapping is null)
        {
            return null;
        }

        mapping.RoleId = roleId;
        return await _rolesRepository.UpdateGroupRoleMappingAsync(mapping, ct);
    }

    /// <summary>
    /// Deletes a group-role mapping by ID.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteGroupMappingAsync(Guid id, CancellationToken ct = default)
        => _rolesRepository.DeleteGroupRoleMappingAsync(id, ct);

    private async Task<GroupRoleMappingResponse?> ResolveGroupRoleMappingAsync(GroupRoleMapping mapping, CancellationToken ct)
    {
        try
        {
            var group = await _graphService.GetGroupByIdAsync(mapping.GroupId.ToString());

            return new GroupRoleMappingResponse
            {
                Id = mapping.Id,
                Group = new GroupResponse
                {
                    Id = group?.Id ?? mapping.GroupId.ToString(),
                    DisplayName = group?.DisplayName ?? mapping.GroupId.ToString(),
                    MailNickname = group?.MailNickname,
                    Mail = group?.Mail,
                    Description = group?.Description,
                    SecurityEnabled = group?.SecurityEnabled
                },
                Role = new RoleResponse
                {
                    Id = mapping.Role?.Id ?? Guid.Empty,
                    Name = mapping.Role?.Name ?? string.Empty,
                    Description = mapping.Role?.Description,
                    CreatedAt = mapping.Role?.CreatedAt ?? default,
                    Permissions = mapping.Role?.RolePermissions
                        .Where(rp => rp.Permission != null)
                        .Select(rp => new PermissionResponse
                        {
                            Id = rp.Permission!.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            CreatedAt = rp.Permission.CreatedAt
                        })
                        .ToList() ?? []
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
}
