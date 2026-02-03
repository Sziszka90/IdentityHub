namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration options for Redis cache
/// </summary>
public class RedisCacheOptions
{
    public const string SectionName = "RedisCache";

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether Redis caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Default cache expiration in seconds
    /// </summary>
    public int DefaultExpirationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// User permissions cache expiration in seconds
    /// </summary>
    public int UserPermissionsExpirationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Role permissions cache expiration in seconds (longer since they change less)
    /// </summary>
    public int RolePermissionsExpirationSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Graph API data cache expiration in seconds
    /// </summary>
    public int GraphDataExpirationSeconds { get; set; } = 600; // 10 minutes
}
