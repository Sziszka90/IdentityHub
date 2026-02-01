namespace IdentityHub.API.DTOs;

/// <summary>
/// Request to check if user has a specific permission
/// </summary>
public record PermissionCheckRequest
{
    public string Permission { get; init; } = string.Empty;
}
