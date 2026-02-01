using IdentityHub.Application.Interfaces;
using IdentityHub.API.DTOs;
using IdentityHub.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

/// <summary>
/// Authorization testing and permission checking endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[ValidateTenant]
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
}
