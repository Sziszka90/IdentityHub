namespace IdentityHub.Contracts.DTOs.Identity.Responses;

/// <summary>
/// Lightweight authentication status for the current user.
/// </summary>
public class AuthStatusResponse
{
    public bool Authenticated { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
