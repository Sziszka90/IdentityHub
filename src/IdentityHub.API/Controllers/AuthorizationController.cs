using IdentityHub.Application.Interfaces;
using IdentityHub.Contracts.DTOs.Permissions.Requests;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

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

    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userContext = await _userContextService.GetUserContext(User);
        var hasPermission = await _userService.UserHasPermissionAsync(userContext.UserId, request.Permission);

        var result = new PermissionCheckResponse
        {
            UserId = userContext.UserId,
            Permission = request.Permission,
            Allowed = hasPermission,
            Reason = hasPermission
                ? $"User has permission '{request.Permission}' or a matching wildcard"
                : $"User does not have permission '{request.Permission}'"
        };

        return Ok(result);
    }
}
