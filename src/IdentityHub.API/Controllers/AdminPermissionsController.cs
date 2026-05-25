using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.Permissions.Requests;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/admin/permissions")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public AdminPermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [RequirePermission("permissions.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(new { count = permissions.Count, permissions });
    }

    [HttpGet("{permissionName}")]
    [RequirePermission("permissions.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionByName(string permissionName)
    {
        var permission = await _permissionService.GetPermissionByNameAsync(permissionName);
        if (permission is null)
        {
            return NotFound(new { message = $"Permission {permissionName} not found" });
        }

        return Ok(permission);
    }

    [HttpPost]
    [RequirePermission("permissions.create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var permission = await _permissionService.CreatePermissionAsync(request.Name);
        if (permission is null)
        {
            return BadRequest(new { message = "Failed to create permission" });
        }

        return CreatedAtAction(nameof(GetPermissionByName), new { permissionName = request.Name }, permission);
    }

    [HttpDelete("{permissionName}")]
    [RequirePermission("permissions.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermission(string permissionName)
    {
        var deleted = await _permissionService.DeletePermissionAsync(permissionName);
        if (!deleted)
        {
            return NotFound(new { message = $"Permission {permissionName} not found" });
        }

        return NoContent();
    }
}
