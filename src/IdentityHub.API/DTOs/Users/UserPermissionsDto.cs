namespace IdentityHub.API.DTOs.Users;

/// <summary>
/// Represents a user's effective permissions
/// </summary>
public class UserPermissionsDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Groups { get; set; } = [];
}
