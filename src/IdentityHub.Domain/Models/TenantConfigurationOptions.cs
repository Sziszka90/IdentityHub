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
    /// Tenant identifier used during startup seeding when no request context exists.
    /// </summary>
    public string SeedTenantId { get; set; } = string.Empty;

    /// <summary>
    /// Optional allow-list of accepted tenant IDs.
    /// </summary>
    public List<string> AllowedTenantIds { get; set; } = [];

    /// <summary>
    /// Whether the app should seed authorization data during startup.
    /// </summary>
    public bool EnableStartupSeeding { get; set; } = true;
}
