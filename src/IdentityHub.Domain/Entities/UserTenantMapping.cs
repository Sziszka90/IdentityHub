namespace IdentityHub.Domain.Entities;

/// <summary>
/// Tracks which Graph user IDs were created in the local database for a tenant.
/// </summary>
public class UserTenantMapping : ITenantOwnedEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Microsoft Graph user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant that owns this user mapping.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
