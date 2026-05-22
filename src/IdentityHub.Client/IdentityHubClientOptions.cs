namespace IdentityHub.Client;

/// <summary>
/// Configuration options for the IdentityHub HTTP client.
/// </summary>
public class IdentityHubClientOptions
{
    public const string SectionName = "IdentityHubClient";

    /// <summary>Base URL of the IdentityHub.API service, e.g. https://identityhub.example.com</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional bearer token for machine-to-machine calls.
    /// Leave empty if the outgoing request already carries a user token.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Cache TTL in seconds for the authorization config snapshot (default: 300).</summary>
    public int CacheSeconds { get; set; } = 300;

    /// <summary>
    /// Cache TTL in seconds for permission authorization decisions.
    /// </summary>
    public int PermissionCheckCacheSeconds { get; set; } = 30;

    /// <summary>
    /// Cache backend to use for IdentityHub client data.
    /// </summary>
    public IdentityHubCacheProvider CacheProvider { get; set; } = IdentityHubCacheProvider.Memory;

    /// <summary>
    /// Redis connection string used when <see cref="CacheProvider"/> is <see cref="IdentityHubCacheProvider.Distributed"/>.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Optional Redis instance name prefix.
    /// </summary>
    public string? RedisInstanceName { get; set; }

    /// <summary>
    /// Cache key prefix shared by all IdentityHub client entries.
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "IdentityHubClient";
}
