namespace IdentityHub.Domain.Entities;

/// <summary>
/// Marks an entity as belonging to a specific tenant.
/// </summary>
public interface ITenantOwnedEntity
{
    /// <summary>
    /// The tenant that owns the entity.
    /// </summary>
    string TenantId { get; set; }
}
