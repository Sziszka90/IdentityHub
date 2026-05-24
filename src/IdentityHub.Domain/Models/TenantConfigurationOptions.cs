namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration for tenant-aware request handling and bootstrapping.
/// </summary>
public class TenantConfigurationOptions
{
    public const string SectionName = "TenantConfiguration";

    /// <summary>
    /// Header name used when a tenant is forwarded explicitly.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Tenant identifiers used during startup seeding.
    /// </summary>
    public List<string> SeedTenantIds { get; set; } = [];

    /// <summary>
    /// Optional allow-list of accepted tenant IDs.
    /// </summary>
    public List<string> AllowedTenantIds { get; set; } = [];

    /// <summary>
    /// Group-to-role mappings to seed for each configured tenant.
    /// </summary>
    public List<SeedGroupRoleMappingOptions> SeedGroupRoleMappings { get; set; } = [];

    /// <summary>
    /// User-to-tenant mappings to seed.
    /// </summary>
    public List<SeedUserTenantMappingOptions> SeedUserTenantMappings { get; set; } = [];

    /// <summary>
    /// Whether the app should seed authorization data during startup.
    /// </summary>
    public bool EnableStartupSeeding { get; set; } = true;
}
