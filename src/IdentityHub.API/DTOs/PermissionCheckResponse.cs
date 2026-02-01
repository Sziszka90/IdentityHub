namespace IdentityHub.API.DTOs;

/// <summary>
/// Response for a permission check operation
/// </summary>
public record PermissionCheckResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Permission { get; init; } = string.Empty;
    public bool Allowed { get; init; }
    public string Reason { get; init; } = string.Empty;
}
