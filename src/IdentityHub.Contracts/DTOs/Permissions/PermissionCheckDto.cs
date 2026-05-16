namespace IdentityHub.Contracts.DTOs.Permissions;

/// <summary>
/// Result of a permission check operation.
/// </summary>
public class PermissionCheckDto
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
}
