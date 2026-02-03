namespace IdentityHub.Domain.Models;

/// <summary>
/// Represents a user's effective permissions
/// </summary>
public class UserPermissionsDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<string> Groups { get; set; } = new();
}

/// <summary>
/// Represents the full resolution chain for a user's permissions
/// </summary>
public class PermissionResolutionChainDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<GroupResolution> GroupResolutions { get; set; } = new();
    public List<string> EffectiveRoles { get; set; } = new();
    public List<string> EffectivePermissions { get; set; } = new();
}

/// <summary>
/// Shows how a group maps to roles and permissions
/// </summary>
public class GroupResolution
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string? MappedRole { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Represents a role and its permissions
/// </summary>
public class RolePermissionsDto
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
