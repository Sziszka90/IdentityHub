using IdentityHub.Application.Interfaces;
using IdentityHub.API.DTOs;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

/// <summary>
/// CRUD endpoints for managing authorization configuration
/// (roles, permissions, group-role mappings, policies).
/// </summary>
[ApiController]
[Route("api/authorization-config")]
[Authorize(Policy = "RequireAdmin")]
public class AuthorizationConfigController : ControllerBase
{
    private readonly IAuthorizationConfigRepository _repo;
    private readonly ILogger<AuthorizationConfigController> _logger;

    public AuthorizationConfigController(
        IAuthorizationConfigRepository repo,
        ILogger<AuthorizationConfigController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════
    //  Full snapshot
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the complete authorization configuration snapshot.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AuthorizationConfigResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFullConfig(CancellationToken ct)
    {
        var rolePerms = await _repo.GetAllRolePermissionsAsync(ct);
        var groupMappings = await _repo.GetGroupToRoleDictionaryAsync(ct);
        var permPolicies = await _repo.GetAllPermissionPoliciesAsync(ct);
        var rolePolicies = await _repo.GetAllRolePoliciesAsync(ct);

        return Ok(new AuthorizationConfigResponse
        {
            RolePermissions = rolePerms,
            GroupToRoleMapping = groupMappings,
            PermissionPolicies = permPolicies.ToDictionary(p => p.PolicyName, p => p.RequiredPermission),
            RolePolicies = rolePolicies.ToDictionary(p => p.PolicyName, p => p.RequiredRoles)
        });
    }

    // ══════════════════════════════════════════════════════════════
    //  Roles
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get all roles with their permissions.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _repo.GetAllRolesAsync(ct);

        return Ok(roles.Select(r => new RoleResponse
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }));
    }

    /// <summary>Get a role by name.</summary>
    [HttpGet("roles/{name}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(string name, CancellationToken ct)
    {
        var role = await _repo.GetRoleByNameAsync(name, ct);
        if (role is null) return NotFound(new { message = $"Role '{name}' not found" });

        return Ok(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    /// <summary>Create a new role with permissions.</summary>
    [HttpPost("roles")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetRoleByNameAsync(request.Name, ct);
        if (existing is not null)
            return Conflict(new { message = $"Role '{request.Name}' already exists" });

        var role = await _repo.CreateRoleAsync(new Role
        {
            Name = request.Name,
            Description = request.Description
        }, ct);

        if (request.Permissions.Count > 0)
            await _repo.SetRolePermissionsAsync(role.Name, request.Permissions, ct);

        // Reload to include permissions
        role = (await _repo.GetRoleByNameAsync(role.Name, ct))!;

        _logger.LogInformation("Created role {RoleName} with {Count} permissions",
            role.Name, request.Permissions.Count);

        return CreatedAtAction(nameof(GetRole), new { name = role.Name }, new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    /// <summary>Update a role's description and/or permissions.</summary>
    [HttpPut("roles/{name}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string name, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _repo.GetRoleByNameAsync(name, ct);
        if (role is null) return NotFound(new { message = $"Role '{name}' not found" });

        role.Description = request.Description;
        await _repo.UpdateRoleAsync(role, ct);
        await _repo.SetRolePermissionsAsync(name, request.Permissions, ct);

        // Reload
        role = (await _repo.GetRoleByNameAsync(name, ct))!;

        _logger.LogInformation("Updated role {RoleName}", name);

        return Ok(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        });
    }

    /// <summary>Delete a role.</summary>
    [HttpDelete("roles/{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(string name, CancellationToken ct)
    {
        var role = await _repo.GetRoleByNameAsync(name, ct);
        if (role is null) return NotFound(new { message = $"Role '{name}' not found" });

        await _repo.DeleteRoleAsync(role.Id, ct);
        _logger.LogInformation("Deleted role {RoleName}", name);

        return NoContent();
    }

    // ══════════════════════════════════════════════════════════════
    //  Group-Role Mappings
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get all group-to-role mappings.</summary>
    [HttpGet("group-mappings")]
    [ProducesResponseType(typeof(List<GroupRoleMappingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupMappings(CancellationToken ct)
    {
        var mappings = await _repo.GetAllGroupRoleMappingsAsync(ct);
        return Ok(mappings.Select(m => new GroupRoleMappingResponse
        {
            Id = m.Id,
            GroupName = m.GroupName,
            RoleName = m.Role.Name,
            CreatedAt = m.CreatedAt
        }));
    }

    /// <summary>Create a group-to-role mapping.</summary>
    [HttpPost("group-mappings")]
    [ProducesResponseType(typeof(GroupRoleMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroupMapping([FromBody] CreateGroupRoleMappingRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetGroupRoleMappingByGroupNameAsync(request.GroupName, ct);
        if (existing is not null)
            return Conflict(new { message = $"Mapping for group '{request.GroupName}' already exists" });

        var role = await _repo.GetRoleByNameAsync(request.RoleName, ct);
        if (role is null)
            return BadRequest(new { message = $"Role '{request.RoleName}' not found" });

        var mapping = await _repo.CreateGroupRoleMappingAsync(new GroupRoleMapping
        {
            GroupName = request.GroupName,
            RoleId = role.Id
        }, ct);

        _logger.LogInformation("Created group mapping: {Group} → {Role}", request.GroupName, request.RoleName);

        return CreatedAtAction(nameof(GetGroupMappings), null, new GroupRoleMappingResponse
        {
            Id = mapping.Id,
            GroupName = mapping.GroupName,
            RoleName = role.Name,
            CreatedAt = mapping.CreatedAt
        });
    }

    /// <summary>Update a group mapping's target role.</summary>
    [HttpPut("group-mappings/{id:int}")]
    [ProducesResponseType(typeof(GroupRoleMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupMapping(int id, [FromBody] UpdateGroupRoleMappingRequest request, CancellationToken ct)
    {
        var mappings = await _repo.GetAllGroupRoleMappingsAsync(ct);
        var mapping = mappings.FirstOrDefault(m => m.Id == id);
        if (mapping is null) return NotFound(new { message = $"Group mapping with id {id} not found" });

        var role = await _repo.GetRoleByNameAsync(request.RoleName, ct);
        if (role is null) return BadRequest(new { message = $"Role '{request.RoleName}' not found" });

        // Re-fetch tracked entity
        var tracked = await _repo.GetGroupRoleMappingByGroupNameAsync(mapping.GroupName, ct);
        tracked!.RoleId = role.Id;
        await _repo.UpdateGroupRoleMappingAsync(tracked, ct);

        _logger.LogInformation("Updated group mapping {Id}: → {Role}", id, request.RoleName);

        return Ok(new GroupRoleMappingResponse
        {
            Id = tracked.Id,
            GroupName = tracked.GroupName,
            RoleName = role.Name,
            CreatedAt = tracked.CreatedAt
        });
    }

    /// <summary>Delete a group mapping.</summary>
    [HttpDelete("group-mappings/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupMapping(int id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteGroupRoleMappingAsync(id, ct);
        if (!deleted) return NotFound(new { message = $"Group mapping with id {id} not found" });

        _logger.LogInformation("Deleted group mapping {Id}", id);
        return NoContent();
    }

    // ══════════════════════════════════════════════════════════════
    //  Permission Policies
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get all permission policies.</summary>
    [HttpGet("permission-policies")]
    [ProducesResponseType(typeof(List<PermissionPolicyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionPolicies(CancellationToken ct)
    {
        var policies = await _repo.GetAllPermissionPoliciesAsync(ct);
        return Ok(policies.Select(p => new PermissionPolicyResponse
        {
            Id = p.Id,
            PolicyName = p.PolicyName,
            RequiredPermission = p.RequiredPermission,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }));
    }

    /// <summary>Create a permission policy.</summary>
    [HttpPost("permission-policies")]
    [ProducesResponseType(typeof(PermissionPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePermissionPolicy([FromBody] CreatePermissionPolicyRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetPermissionPolicyByNameAsync(request.PolicyName, ct);
        if (existing is not null)
            return Conflict(new { message = $"Permission policy '{request.PolicyName}' already exists" });

        var policy = await _repo.CreatePermissionPolicyAsync(new PermissionPolicy
        {
            PolicyName = request.PolicyName,
            RequiredPermission = request.RequiredPermission
        }, ct);

        _logger.LogInformation("Created permission policy {Name}", request.PolicyName);

        return CreatedAtAction(nameof(GetPermissionPolicies), null, new PermissionPolicyResponse
        {
            Id = policy.Id,
            PolicyName = policy.PolicyName,
            RequiredPermission = policy.RequiredPermission,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        });
    }

    /// <summary>Update a permission policy.</summary>
    [HttpPut("permission-policies/{id:int}")]
    [ProducesResponseType(typeof(PermissionPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermissionPolicy(int id, [FromBody] UpdatePermissionPolicyRequest request, CancellationToken ct)
    {
        var policies = await _repo.GetAllPermissionPoliciesAsync(ct);
        var policy = policies.FirstOrDefault(p => p.Id == id);
        if (policy is null) return NotFound(new { message = $"Permission policy with id {id} not found" });

        // Re-fetch tracked
        var tracked = (await _repo.GetPermissionPolicyByNameAsync(policy.PolicyName, ct))!;
        tracked.RequiredPermission = request.RequiredPermission;
        await _repo.UpdatePermissionPolicyAsync(tracked, ct);

        _logger.LogInformation("Updated permission policy {Id}", id);

        return Ok(new PermissionPolicyResponse
        {
            Id = tracked.Id,
            PolicyName = tracked.PolicyName,
            RequiredPermission = tracked.RequiredPermission,
            CreatedAt = tracked.CreatedAt,
            UpdatedAt = tracked.UpdatedAt
        });
    }

    /// <summary>Delete a permission policy.</summary>
    [HttpDelete("permission-policies/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermissionPolicy(int id, CancellationToken ct)
    {
        var deleted = await _repo.DeletePermissionPolicyAsync(id, ct);
        if (!deleted) return NotFound(new { message = $"Permission policy with id {id} not found" });

        _logger.LogInformation("Deleted permission policy {Id}", id);
        return NoContent();
    }

    // ══════════════════════════════════════════════════════════════
    //  Role Policies
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get all role policies.</summary>
    [HttpGet("role-policies")]
    [ProducesResponseType(typeof(List<RolePolicyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePolicies(CancellationToken ct)
    {
        var policies = await _repo.GetAllRolePoliciesAsync(ct);
        return Ok(policies.Select(p => new RolePolicyResponse
        {
            Id = p.Id,
            PolicyName = p.PolicyName,
            RequiredRoles = p.RequiredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }));
    }

    /// <summary>Create a role policy.</summary>
    [HttpPost("role-policies")]
    [ProducesResponseType(typeof(RolePolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRolePolicy([FromBody] CreateRolePolicyRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetRolePolicyByNameAsync(request.PolicyName, ct);
        if (existing is not null)
            return Conflict(new { message = $"Role policy '{request.PolicyName}' already exists" });

        var policy = await _repo.CreateRolePolicyAsync(new RolePolicy
        {
            PolicyName = request.PolicyName,
            RequiredRoles = string.Join(",", request.RequiredRoles)
        }, ct);

        _logger.LogInformation("Created role policy {Name}", request.PolicyName);

        return CreatedAtAction(nameof(GetRolePolicies), null, new RolePolicyResponse
        {
            Id = policy.Id,
            PolicyName = policy.PolicyName,
            RequiredRoles = request.RequiredRoles,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        });
    }

    /// <summary>Update a role policy.</summary>
    [HttpPut("role-policies/{id:int}")]
    [ProducesResponseType(typeof(RolePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePolicy(int id, [FromBody] UpdateRolePolicyRequest request, CancellationToken ct)
    {
        var policies = await _repo.GetAllRolePoliciesAsync(ct);
        var policy = policies.FirstOrDefault(p => p.Id == id);
        if (policy is null) return NotFound(new { message = $"Role policy with id {id} not found" });

        var tracked = (await _repo.GetRolePolicyByNameAsync(policy.PolicyName, ct))!;
        tracked.RequiredRoles = string.Join(",", request.RequiredRoles);
        await _repo.UpdateRolePolicyAsync(tracked, ct);

        _logger.LogInformation("Updated role policy {Id}", id);

        return Ok(new RolePolicyResponse
        {
            Id = tracked.Id,
            PolicyName = tracked.PolicyName,
            RequiredRoles = request.RequiredRoles,
            CreatedAt = tracked.CreatedAt,
            UpdatedAt = tracked.UpdatedAt
        });
    }

    /// <summary>Delete a role policy.</summary>
    [HttpDelete("role-policies/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRolePolicy(int id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteRolePolicyAsync(id, ct);
        if (!deleted) return NotFound(new { message = $"Role policy with id {id} not found" });

        _logger.LogInformation("Deleted role policy {Id}", id);
        return NoContent();
    }
}
