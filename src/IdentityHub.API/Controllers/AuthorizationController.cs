using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using IdentityHub.Contracts.DTOs.Permissions.Requests;

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
    private readonly IUserService _userService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IUserContextService userContextService,
        IUserService userService,
        ILogger<AuthorizationController> logger)
    {
        _userContextService = userContextService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        var userContext = _userContextService.GetUserContext(User);

        var hasPermission = await _userService.UserHasPermissionAsync(userContext.Id.ToString(), request.Permission);

        var result = new PermissionCheckResponse
        {
            UserId = userContext.Id.ToString(),
            Permission = request.Permission,
            Allowed = hasPermission,
            Reason = hasPermission
                ? $"User has permission '{request.Permission}' or a matching wildcard"
                : $"User does not have permission '{request.Permission}'"
        };

        _logger.LogInformation(
            "Permission check: User {UserId} - Permission {Permission} - Result {Result}",
            userContext.Id, request.Permission, hasPermission);

        return Ok(result);
    }
}
