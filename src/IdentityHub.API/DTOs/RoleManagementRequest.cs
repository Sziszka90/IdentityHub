namespace IdentityHub.API.DTOs;

/// <summary>
/// Request to create or update a role
/// </summary>
public class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Request to update role permissions
/// </summary>
public class UpdateRolePermissionsRequest
{
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Request to assign/remove roles to/from a user
/// </summary>
public class AssignRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Request to assign/remove a user to/from groups
/// </summary>
public class AssignGroupsRequest
{
    public List<string> GroupIds { get; set; } = new();
}
