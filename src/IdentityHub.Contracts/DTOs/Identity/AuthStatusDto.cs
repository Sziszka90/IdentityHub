namespace IdentityHub.Contracts.DTOs.Identity;

/// <summary>
/// Lightweight authentication status for the current user.
/// </summary>
public class AuthStatusDto
{
    public bool Authenticated { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
