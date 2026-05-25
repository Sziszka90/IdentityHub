using AutoMapper;
using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.Roles.Requests;
using IdentityHub.Contracts.DTOs.Roles.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IMapper _mapper;

    public AdminRolesController(IRoleService roleService, IMapper mapper)
    {
        _roleService = roleService;
        _mapper = mapper;
    }

    [HttpGet]
    [RequirePermission("roles.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        var roleResponses = _mapper.Map<List<RoleResponse>>(roles);

        return Ok(new { count = roleResponses.Count, roles = roleResponses });
    }

    [HttpGet("{roleId}")]
    [RequirePermission("roles.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(string roleId)
    {
        if (!Guid.TryParse(roleId, out var parsedRoleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {roleId}" });
        }

        var role = await _roleService.GetRoleByIdAsync(parsedRoleId);
        if (role is null)
        {
            return NotFound(new { message = $"Role {roleId} not found" });
        }

        return Ok(_mapper.Map<RoleResponse>(role));
    }

    [HttpPost]
    [RequirePermission("roles.create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = await _roleService.CreateRoleAsync(request.Name, request.Description, request.Permissions);
        if (role is null)
        {
            return BadRequest(new { message = "Failed to create role" });
        }

        return CreatedAtAction(nameof(GetRoleById), new { roleId = role.Id }, _mapper.Map<RoleResponse>(role));
    }

    [HttpPut("{roleId}")]
    [RequirePermission("roles.update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!Guid.TryParse(roleId, out var parsedRoleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {roleId}" });
        }

        var role = await _roleService.UpdateRoleAsync(parsedRoleId, request.Description, request.Permissions);
        if (role is null)
        {
            return NotFound(new { message = $"Role {roleId} not found" });
        }

        return Ok(_mapper.Map<RoleResponse>(role));
    }

    [HttpDelete("{roleId}")]
    [RequirePermission("roles.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        if (!Guid.TryParse(roleId, out var parsedRoleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {roleId}" });
        }

        var deleted = await _roleService.DeleteRoleAsync(parsedRoleId);
        if (!deleted)
        {
            return NotFound(new { message = $"Role {roleId} not found" });
        }

        return NoContent();
    }
}
