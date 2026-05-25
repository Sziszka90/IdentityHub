using AutoMapper;
using IdentityHub.Application.Interfaces;
using IdentityHub.Contracts.DTOs.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IdentityController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly IMapper _mapper;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IUserContextService userContextService,
        IMapper mapper,
        ILogger<IdentityController> logger)
    {
        _userContextService = userContextService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userContext = await _userContextService.GetUserContext(User);

        if (!_userContextService.ValidateUserContext(userContext))
        {
            return Unauthorized(new { error = "Invalid user context" });
        }

        return Ok(_mapper.Map<UserResponse>(userContext));
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAuthStatus()
    {
        var userContext = await _userContextService.GetUserContext(User);

        return Ok(new
        {
            authenticated = userContext.IsAuthenticated,
            userId = userContext.UserId,
            tenantId = userContext.TenantId,
            timestamp = DateTime.UtcNow
        });
    }
}
