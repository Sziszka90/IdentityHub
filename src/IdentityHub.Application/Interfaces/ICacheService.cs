namespace IdentityHub.Application.Interfaces;

/// <summary>
/// Service for distributed caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set a cached value
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expirationSeconds">Expiration in seconds (null for default)</param>
    Task SetAsync<T>(string key, T value, int? expirationSeconds = null) where T : class;

    /// <summary>
    /// Remove a cached value
    /// </summary>
    /// <param name="key">Cache key</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove all cached values matching a pattern
    /// </summary>
    /// <param name="pattern">Key pattern (e.g., "user:*")</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Check if Redis is available
    /// </summary>
    /// <returns>True if Redis is available</returns>
    bool IsAvailable();
}
