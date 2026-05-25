using AutoMapper;
using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.Users.Requests;
using IdentityHub.Contracts.DTOs.Users.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGraphService _graphService;
    private readonly IMapper _mapper;

    public AdminUsersController(
        IUserService userService,
        IGraphService graphService,
        IMapper mapper)
    {
        _userService = userService;
        _graphService = graphService;
        _mapper = mapper;
    }

    [HttpGet]
    [RequirePermission("users.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersWithPermissionsAsync();
        return Ok(new { count = users.Count, users });
    }

    [HttpGet("{userId}")]
    [RequirePermission("users.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string userId)
    {
        var user = await _graphService.GetUserAsync(userId);
        if (user is null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        return Ok(_mapper.Map<UserResponse>(user));
    }

    [HttpPost]
    [RequirePermission("users.create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = _mapper.Map<User>(request);
        var createdUser = await _userService.CreateUserWithRolesAsync(user, request.RoleIds);
        if (createdUser is null)
        {
            return BadRequest(new { message = "Failed to create user" });
        }

        return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.Id }, _mapper.Map<UserResponse>(createdUser));
    }

    [HttpPut("{userId}")]
    [RequirePermission("users.update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updateUser = new User
        {
            DisplayName = request.DisplayName,
            AccountEnabled = request.AccountEnabled,
            JobTitle = request.JobTitle,
            Department = request.Department,
            OfficeLocation = request.OfficeLocation
        };

        var updatedUser = await _userService.UpdateUserWithRolesAsync(updateUser, userId, request.RoleIds);
        if (updatedUser is null)
        {
            return NotFound(new { message = $"User {userId} not found or update failed" });
        }

        return Ok(_mapper.Map<UserResponse>(updatedUser));
    }

    [HttpDelete("{userId}")]
    [RequirePermission("users.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        await _graphService.DeleteUserAsync(userId);
        return NoContent();
    }

    [HttpGet("{userId}/permissions")]
    [RequirePermission("users.permissions.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        var userPermissions = await _userService.GetUserPermissionsAsync(userId);
        if (userPermissions is null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        return Ok(userPermissions);
    }

    [HttpGet("{userId}/resolution-chain")]
    [RequirePermission("users.permissions.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionResolutionChain(string userId)
    {
        var resolutionChain = await _userService.GetPermissionResolutionChainAsync(userId);
        if (resolutionChain is null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        return Ok(resolutionChain);
    }

    [HttpPost("{userId}/roles")]
    [RequirePermission("users.roles.assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRolesToUser(string userId, [FromBody] List<string> roleIds)
    {
        if (roleIds is null || roleIds.Count == 0)
        {
            return BadRequest(new { message = "At least one role ID must be specified" });
        }

        var userPermissions = await _userService.AssignRolesToUserAsync(userId, roleIds);
        if (userPermissions is null)
        {
            return NotFound(new { message = $"User {userId} not found or invalid role IDs" });
        }

        return Ok(userPermissions);
    }

    [HttpDelete("{userId}/roles")]
    [RequirePermission("users.roles.remove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveRolesFromUser(string userId, [FromBody] List<string> roleIds)
    {
        if (roleIds is null || roleIds.Count == 0)
        {
            return BadRequest(new { message = "At least one role ID must be specified" });
        }

        var userPermissions = await _userService.RemoveRolesFromUserAsync(userId, roleIds);
        if (userPermissions is null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        return Ok(userPermissions);
    }
}
