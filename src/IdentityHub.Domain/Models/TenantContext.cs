namespace IdentityHub.Domain.Models;

/// <summary>
/// Tenant context for the current request
/// </summary>
public class TenantContext
{
    /// <summary>
    /// Tenant identifier from JWT token
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Whether tenant context is valid
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(TenantId);

    /// <summary>
    /// User ID associated with this tenant context
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}
