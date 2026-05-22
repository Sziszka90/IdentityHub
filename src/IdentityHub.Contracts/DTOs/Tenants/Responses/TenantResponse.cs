namespace IdentityHub.Contracts.DTOs.Tenants.Responses;

/// <summary>
/// Current tenant context for the active request.
/// </summary>
public class TenantResponse
{
    public string TenantId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public bool IsValid { get; init; }
}
