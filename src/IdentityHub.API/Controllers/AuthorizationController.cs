using IdentityHub.Application.Interfaces;
using IdentityHub.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

/// <summary>
/// Authorization testing and permission checking endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthorizationController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IUserContextService userContextService,
        ILogger<AuthorizationController> logger)
    {
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResponse), StatusCodes.Status200OK)]
    public IActionResult CheckPermission([FromBody] PermissionCheckRequest request)
    {
        var userContext = _userContextService.GetUserContext(User);

        var hasPermission = userContext.HasPermission(request.Permission);

        var result = new PermissionCheckResponse
        {
            UserId = userContext.UserId,
            Permission = request.Permission,
            Allowed = hasPermission,
            Reason = hasPermission
                ? $"User has permission '{request.Permission}' or a matching wildcard"
                : $"User does not have permission '{request.Permission}'"
        };

        _logger.LogInformation(
            "Permission check: User {UserId} - Permission {Permission} - Result {Result}",
            userContext.UserId, request.Permission, hasPermission);

        return Ok(result);
    }

    /// <summary>
    /// Get user's effective permissions
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(UserPermissionsResponse), StatusCodes.Status200OK)]
    public IActionResult GetPermissions()
    {
        var userContext = _userContextService.GetUserContext(User);

        var response = new UserPermissionsResponse
        {
            UserId = userContext.UserId,
            Email = userContext.Email,
            TenantId = userContext.TenantId,
            Groups = userContext.Groups,
            Roles = userContext.Roles,
            Permissions = userContext.Permissions
        };

        return Ok(response);
    }

    /// <summary>
    /// Test endpoint - requires Admin role
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You have Admin access!" });
    }

    /// <summary>
    /// Test endpoint - requires users.delete permission
    /// </summary>
    [HttpGet("can-delete-users")]
    [Authorize(Policy = "CanDeleteUsers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CanDeleteUsers()
    {
        return Ok(new { message = "You can delete users!" });
    }

    /// <summary>
    /// Test endpoint - requires tickets.assign permission
    /// </summary>
    [HttpGet("can-assign-tickets")]
    [Authorize(Policy = "CanAssignTickets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CanAssignTickets()
    {
        return Ok(new { message = "You can assign tickets!" });
    }
}
