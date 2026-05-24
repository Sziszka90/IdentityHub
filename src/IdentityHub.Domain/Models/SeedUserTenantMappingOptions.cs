namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration entry describing a user-to-tenant mapping to seed.
/// </summary>
public class SeedUserTenantMappingOptions
{
    public string UserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
