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

    /// <summary>
    /// Get current authenticated user's identity context
    /// </summary>
    /// <returns>User context with identity information</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userContext = await _userContextService.GetUserContext(User);

            if (!_userContextService.ValidateUserContext(userContext))
            {
                _logger.LogWarning("Invalid user context for user {UserId}", userContext.UserId);
                return Unauthorized(new { error = "Invalid user context" });
            }

            _logger.LogInformation("User {UserId} from tenant {TenantId} retrieved their identity",
                userContext.UserId, userContext.TenantId);

            return Ok(_mapper.Map<UserResponse>(userContext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user context");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving user context" });
        }
    }

    /// <summary>
    /// Health check endpoint to verify authentication is working
    /// </summary>
    /// <returns>Simple status message</returns>
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
