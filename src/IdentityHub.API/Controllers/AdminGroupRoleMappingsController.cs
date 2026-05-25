using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.GroupRoleMappings.Requests;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/admin/group-role-mappings")]
public class AdminGroupRoleMappingsController : ControllerBase
{
    private readonly IRoleService _roleService;

    public AdminGroupRoleMappingsController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupRoleMappings()
    {
        var groupRoleMappings = await _roleService.GetAllGroupMappingsAsync();
        return Ok(new { count = groupRoleMappings.Count, groupRoleMappings });
    }

    [HttpGet("{groupId}")]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupRoleMappingByGroupId(string groupId)
    {
        var groupRoleMapping = await _roleService.GetGroupMappingByGroupNameAsync(groupId);
        if (groupRoleMapping is null)
        {
            return NotFound(new { message = $"Group-role mapping for group {groupId} not found" });
        }

        return Ok(groupRoleMapping);
    }

    [HttpPost]
    [RequirePermission("groups.create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroupRoleMapping([FromBody] CreateGroupRoleMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {request.RoleId}" });
        }

        if (!Guid.TryParse(request.GroupId, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {request.GroupId}" });
        }

        var groupRoleMapping = await _roleService.CreateGroupMappingAsync(groupId, roleId);
        if (groupRoleMapping is null)
        {
            return BadRequest(new { message = "Failed to create group-role mapping" });
        }

        return CreatedAtAction(nameof(GetGroupRoleMappingByGroupId), new { groupId = request.GroupId }, groupRoleMapping);
    }

    [HttpPut("{id}")]
    [RequirePermission("groups.update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupRoleMapping(string id, [FromBody] UpdateGroupRoleMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!Guid.TryParse(id, out var mappingId))
        {
            return BadRequest(new { message = $"Invalid mapping ID: {id}" });
        }

        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {request.RoleId}" });
        }

        if (!Guid.TryParse(request.GroupId, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {request.GroupId}" });
        }

        var groupRoleMapping = await _roleService.UpdateGroupMappingAsync(mappingId, groupId, roleId);
        if (groupRoleMapping is null)
        {
            return NotFound(new { message = $"Group-role mapping {id} not found" });
        }

        return Ok(groupRoleMapping);
    }

    [HttpDelete("{id}")]
    [RequirePermission("groups.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupRoleMapping(string id)
    {
        if (!Guid.TryParse(id, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {id}" });
        }

        var deleted = await _roleService.DeleteGroupMappingAsync(groupId);
        if (!deleted)
        {
            return NotFound(new { message = $"Group-role mapping {id} not found" });
        }

        return NoContent();
    }
}
