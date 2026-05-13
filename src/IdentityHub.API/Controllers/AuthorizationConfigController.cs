using IdentityHub.API.DTOs.AuthorizationConfig.Requests;
using IdentityHub.API.DTOs.AuthorizationConfig.Responses;
using IdentityHub.Application.Interfaces;
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
    private readonly IAuthorizationConfigService _service;
    private readonly ILogger<AuthorizationConfigController> _logger;

    public AuthorizationConfigController(
        IAuthorizationConfigService service,
        ILogger<AuthorizationConfigController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get the complete authorization configuration snapshot (roles, permissions, group-role mappings).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AuthorizationConfigResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFullConfig(CancellationToken ct)
    {
        var rolePerms = await _service.GetAllRolePermissionsAsync(ct);
        var groupMappings = await _service.GetGroupToRoleDictionaryAsync(ct);

        return Ok(new AuthorizationConfigResponse
        {
            RolePermissions = rolePerms,
            GroupToRoleMapping = groupMappings
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
        var roles = await _service.GetAllRolesAsync(ct);

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
        var role = await _service.GetRoleByNameAsync(name, ct);
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
        var role = await _service.CreateRoleAsync(request.Name, request.Description, request.Permissions, ct);
        if (role is null)
            return Conflict(new { message = $"Role '{request.Name}' already exists" });

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
        var role = await _service.UpdateRoleAsync(name, request.Description, request.Permissions, ct);
        if (role is null) return NotFound(new { message = $"Role '{name}' not found" });

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
        var deleted = await _service.DeleteRoleAsync(name, ct);
        if (!deleted) return NotFound(new { message = $"Role '{name}' not found" });

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
        var mappings = await _service.GetAllGroupMappingsAsync(ct);
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
        var existing = await _service.GetGroupMappingByGroupNameAsync(request.GroupName, ct);
        if (existing is not null)
            return Conflict(new { message = $"Mapping for group '{request.GroupName}' already exists" });

        var role = await _service.GetRoleByNameAsync(request.RoleName, ct);
        if (role is null)
            return BadRequest(new { message = $"Role '{request.RoleName}' not found" });

        var mapping = await _service.CreateGroupMappingAsync(request.GroupName, role.Id, ct);

        _logger.LogInformation("Created group mapping: {Group} → {Role}", request.GroupName, request.RoleName);

        return CreatedAtAction(nameof(GetGroupMappings), null, new GroupRoleMappingResponse
        {
            Id = mapping!.Id,
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
        var role = await _service.GetRoleByNameAsync(request.RoleName, ct);
        if (role is null) return BadRequest(new { message = $"Role '{request.RoleName}' not found" });

        var updated = await _service.UpdateGroupMappingAsync(id, role.Id, ct);
        if (updated is null) return NotFound(new { message = $"Group mapping with id {id} not found" });

        _logger.LogInformation("Updated group mapping {Id}: → {Role}", id, request.RoleName);

        return Ok(new GroupRoleMappingResponse
        {
            Id = updated.Id,
            GroupName = updated.GroupName,
            RoleName = role.Name,
            CreatedAt = updated.CreatedAt
        });
    }

    /// <summary>Delete a group mapping.</summary>
    [HttpDelete("group-mappings/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupMapping(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteGroupMappingAsync(id, ct);
        if (!deleted) return NotFound(new { message = $"Group mapping with id {id} not found" });

        _logger.LogInformation("Deleted group mapping {Id}", id);
        return NoContent();
    }


}
