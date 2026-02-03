using IdentityHub.Application.Interfaces;
using IdentityHub.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

/// <summary>
/// Admin endpoints for managing users, roles, and permissions
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with their effective permissions (tenant-scoped)
    /// </summary>
    /// <returns>List of users with permissions</returns>
    [HttpGet("users")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Admin requesting all users");

        var users = await _adminService.GetUsersWithPermissionsAsync();
        return Ok(new
        {
            count = users.Count,
            users = users
        });
    }

    /// <summary>
    /// Get a specific user's effective permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User permissions</returns>
    [HttpGet("users/{userId}/permissions")]
    [Authorize(Policy = "RequireAdminOrAgent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        _logger.LogInformation("Admin requesting permissions for user {UserId}", userId);

        var userPermissions = await _adminService.GetUserPermissionsAsync(userId);
        if (userPermissions == null)
        {
            return NotFound(new { message = $"User {userId} not found in current tenant" });
        }

        return Ok(userPermissions);
    }

    /// <summary>
    /// Get detailed permission resolution chain for a user
    /// Shows groups → roles → permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Permission resolution chain</returns>
    [HttpGet("users/{userId}/resolution-chain")]
    [Authorize(Policy = "RequireAdminOrAgent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPermissionResolutionChain(string userId)
    {
        _logger.LogInformation("Admin requesting resolution chain for user {UserId}", userId);

        var resolutionChain = await _adminService.GetPermissionResolutionChainAsync(userId);
        if (resolutionChain == null)
        {
            return NotFound(new { message = $"User {userId} not found in current tenant" });
        }

        return Ok(resolutionChain);
    }

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    /// <returns>List of roles and permissions</returns>
    [HttpGet("roles")]
    [Authorize(Policy = "RequireAdminOrAgent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetRoles()
    {
        _logger.LogInformation("Admin requesting all roles");

        var roles = _adminService.GetAllRolesWithPermissions();
        return Ok(new
        {
            count = roles.Count,
            roles = roles
        });
    }

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Role permissions</returns>
    [HttpGet("roles/{roleName}/permissions")]
    [Authorize(Policy = "RequireAdminOrAgent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetRolePermissions(string roleName)
    {
        _logger.LogInformation("Admin requesting permissions for role {RoleName}", roleName);

        var rolePermissions = _adminService.GetRolePermissions(roleName);
        if (rolePermissions == null)
        {
            return NotFound(new { message = $"Role {roleName} not found" });
        }

        return Ok(rolePermissions);
    }

    /// <summary>
    /// Create a new role with permissions
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <returns>Created role</returns>
    [HttpPost("roles")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrEmpty(request.RoleName))
        {
            return BadRequest(new { message = "Role name is required" });
        }

        _logger.LogInformation("Admin creating role {RoleName}", request.RoleName);

        var role = await _adminService.CreateRoleAsync(request.RoleName, request.Permissions);
        if (role == null)
        {
            return Conflict(new { message = $"Role {request.RoleName} already exists" });
        }

        return CreatedAtAction(
            nameof(GetRolePermissions),
            new { roleName = role.RoleName },
            role);
    }

    /// <summary>
    /// Update an existing role's permissions
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="request">Updated permissions</param>
    /// <returns>Updated role</returns>
    [HttpPut("roles/{roleName}/permissions")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRolePermissions(
        string roleName,
        [FromBody] UpdateRolePermissionsRequest request)
    {
        _logger.LogInformation("Admin updating permissions for role {RoleName}", roleName);

        var role = await _adminService.UpdateRolePermissionsAsync(roleName, request.Permissions);
        if (role == null)
        {
            return NotFound(new { message = $"Role {roleName} not found" });
        }

        return Ok(role);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>No content</returns>
    [HttpDelete("roles/{roleName}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        _logger.LogInformation("Admin deleting role {RoleName}", roleName);

        var deleted = await _adminService.DeleteRoleAsync(roleName);
        if (!deleted)
        {
            return NotFound(new { message = $"Role {roleName} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Roles to assign</param>
    /// <returns>Updated user permissions</returns>
    [HttpPost("users/{userId}/roles")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRolesToUser(
        string userId,
        [FromBody] AssignRolesRequest request)
    {
        if (request.Roles == null || request.Roles.Count == 0)
        {
            return BadRequest(new { message = "At least one role must be specified" });
        }

        _logger.LogInformation("Admin assigning roles to user {UserId}", userId);

        var userPermissions = await _adminService.AssignRolesToUserAsync(userId, request.Roles);
        if (userPermissions == null)
        {
            return NotFound(new { message = $"User {userId} not found or invalid roles" });
        }

        return Ok(userPermissions);
    }

    /// <summary>
    /// Remove roles from a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Roles to remove</param>
    /// <returns>Updated user permissions</returns>
    [HttpDelete("users/{userId}/roles")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRolesFromUser(
        string userId,
        [FromBody] AssignRolesRequest request)
    {
        if (request.Roles == null || request.Roles.Count == 0)
        {
            return BadRequest(new { message = "At least one role must be specified" });
        }

        _logger.LogInformation("Admin removing roles from user {UserId}", userId);

        var userPermissions = await _adminService.RemoveRolesFromUserAsync(userId, request.Roles);
        if (userPermissions == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        return Ok(userPermissions);
    }
}
