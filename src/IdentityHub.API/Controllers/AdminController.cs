using AutoMapper;
using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.GroupRoleMappings.Requests;
using IdentityHub.Contracts.DTOs.Groups.Responses;
using IdentityHub.Contracts.DTOs.Permissions.Requests;
using IdentityHub.Contracts.DTOs.Roles.Requests;
using IdentityHub.Contracts.DTOs.Roles.Responses;
using IdentityHub.Contracts.DTOs.Users.Requests;
using IdentityHub.Contracts.DTOs.Users.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;

namespace IdentityHub.API.Controllers;

/// <summary>
/// Admin endpoints for querying user permissions and managing user role assignments.
/// Role and group-mapping CRUD is handled by <see cref="AuthorizationConfigController"/>.
/// </summary>
[ApiController]
[Route("api/admin")]
// Remove broad role requirement; use fine-grained permission attributes per endpoint
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGraphService _graphService;
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;
    private readonly ILogger<AdminController> _logger;
    private readonly IMapper _mapper;

    public AdminController(
        IUserService userService,
        IGraphService graphService,
        IPermissionService permissionService,
        IRoleService roleService,
        ILogger<AdminController> logger,
        IMapper mapper)
    {
        _userService = userService;
        _graphService = graphService;
        _permissionService = permissionService;
        _roleService = roleService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    // --- USERS CRUD ---
    [HttpGet("users")]
    [RequirePermission("users.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Admin requesting all users");
        var users = await _userService.GetUsersWithPermissionsAsync();
        return Ok(new { count = users.Count, users });
    }

    /// <summary>
    /// Get a user by ID
    /// </summary>
    [HttpGet("users/{userId}")]
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

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost("users")]
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

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("users/{userId}")]
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

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("users/{userId}")]
    [RequirePermission("users.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        await _graphService.DeleteUserAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Get a specific user's permissions
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
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

    /// <summary>
    /// Get detailed permission resolution chain for a user (groups → roles → permissions)
    /// </summary>
    [HttpGet("users/{userId}/resolution-chain")]
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

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
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

    /// <summary>
    /// Remove roles from a user
    /// </summary>
    [HttpDelete("users/{userId}/roles")]
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

    // --- GROUPS CRUD ---
    [HttpGet("groups")]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups([FromQuery] string? displayName = null)
    {
        var groups = await _graphService.QueryGroupsAsync(displayName);
        var groupResponses = groups.Select(group => new GroupResponse
        {
            Id = group.Id ?? string.Empty,
            DisplayName = group.DisplayName ?? string.Empty,
            MailNickname = group.MailNickname,
            Mail = group.Mail,
            Description = group.Description,
            SecurityEnabled = group.SecurityEnabled
        }).ToList();

        return Ok(new { count = groupResponses.Count, groups = groupResponses });
    }

    // --- ROLES CRUD ---
    [HttpGet("roles")]
    [RequirePermission("roles.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        var roleResponses = _mapper.Map<List<RoleResponse>>(roles);
        return Ok(new { count = roleResponses.Count, roles = roleResponses });
    }

    [HttpGet("roles/{roleId}")]
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

        return Ok(role);
    }

    [HttpPost("roles")]
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
        return CreatedAtAction(nameof(GetRoleById), new { roleId = role.Id }, role);
    }

    [HttpPut("roles/{roleId}")]
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
        return Ok(role);
    }

    [HttpDelete("roles/{roleId}")]
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

    // --- PERMISSIONS CRUD ---
    [HttpGet("permissions")]
    [RequirePermission("permissions.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(new { count = permissions.Count, permissions });
    }

    [HttpGet("permissions/{permissionName}")]
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

    [HttpPost("permissions")]
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

    [HttpDelete("permissions/{permissionName}")]
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

    // --- GROUP-ROLE MAPPINGS CRUD ---
    [HttpGet("group-role-mappings")]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupRoleMappings()
    {
        var groupRoleMappings = await _roleService.GetAllGroupMappingsAsync();
        return Ok(new { count = groupRoleMappings.Count, groupRoleMappings });
    }

    [HttpGet("group-role-mappings/{groupName}")]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupRoleMappingByGroupName(string groupName)
    {
        var groupRoleMapping = await _roleService.GetGroupMappingByGroupNameAsync(groupName);
        if (groupRoleMapping is null)
        {
            return NotFound(new { message = $"Group-role mapping for group {groupName} not found" });
        }

        return Ok(groupRoleMapping);
    }

    [HttpPost("group-role-mappings")]
    [RequirePermission("groups.create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroupRoleMapping([FromBody] CreateGroupRoleMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {request.RoleId}" });
        }
        if (!Guid.TryParse(request.GroupId, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {request.GroupId}" });
        }
        var groupRoleMapping = await _roleService.CreateGroupMappingAsync(groupId, roleId);
        if (groupRoleMapping is null)
        {
            return BadRequest(new { message = "Failed to create group-role mapping" });
        }
        return CreatedAtAction(nameof(GetGroupRoleMappingByGroupName), new { groupName = request.GroupId }, groupRoleMapping);
    }

    [HttpPut("group-role-mappings/{id}")]
    [RequirePermission("groups.update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupRoleMapping(string id, [FromBody] UpdateGroupRoleMappingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!Guid.TryParse(id, out var mappingId))
        {
            return BadRequest(new { message = $"Invalid mapping ID: {id}" });
        }

        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            return BadRequest(new { message = $"Invalid role ID: {request.RoleId}" });
        }

        if (!Guid.TryParse(request.GroupId, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {request.GroupId}" });
        }

        var groupRoleMapping = await _roleService.UpdateGroupMappingAsync(mappingId, groupId, roleId);

        if (groupRoleMapping is null)
        {
            return NotFound(new { message = $"Group-role mapping {id} not found" });
        }
        return Ok(groupRoleMapping);
    }

    [HttpDelete("group-role-mappings/{id}")]
    [RequirePermission("groups.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupRoleMapping(string id)
    {
        if (!Guid.TryParse(id, out var groupId))
        {
            return BadRequest(new { message = $"Invalid group ID: {id}" });
        }
        var deleted = await _roleService.DeleteGroupMappingAsync(groupId);
        if (!deleted)
        {
            return NotFound(new { message = $"Group-role mapping {id} not found" });
        }
        return NoContent();
    }
}
