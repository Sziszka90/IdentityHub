namespace IdentityHub.API.DTOs;

/// <summary>
/// Response containing user's effective permissions
/// </summary>
public record UserPermissionsResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public List<string> Groups { get; init; } = new();
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
}
