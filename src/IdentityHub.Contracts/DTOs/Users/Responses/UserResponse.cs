namespace IdentityHub.Contracts.DTOs.Users.Responses;

/// <summary>
/// Response containing user's effective permissions
/// </summary>
public record UserResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public List<string> Groups { get; init; } = new();
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
}
