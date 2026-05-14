namespace IdentityHub.Application.DTOs.Roles;

/// <summary>
/// Represents a role and its permissions
/// </summary>
public class RolePermissionsDto
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}
