namespace IdentityHub.Contracts.DTOs.Identity.Responses;

/// <summary>
/// Represents the authenticated user's identity context.
/// </summary>
public class UserContextResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public List<string> Groups { get; set; } = [];
    public Dictionary<string, string> Claims { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsAuthenticated { get; set; }
}
