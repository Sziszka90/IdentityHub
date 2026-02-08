using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IdentityHub.Application.Services;

/// <summary>
/// Redis-based distributed cache service
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache? _cache;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<CacheService> _logger;
    private readonly bool _isEnabled;

    public CacheService(
        IDistributedCache? cache,
        IOptions<RedisCacheOptions> options,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _isEnabled = _options.Enabled && _cache != null;

        if (!_isEnabled)
        {
            _logger.LogWarning("Redis caching is disabled or not configured");
        }
    }

    /// <summary>
    /// Get a cached value
    /// </summary>
    /// <typeparam name="T">The type of the cached object</typeparam>
    /// <param name="key">The cache key</param>
    /// <returns>The cached object if found, null otherwise</returns>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_isEnabled || _cache is null)
        {
            return null;
        }

        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Set a cached value
    /// </summary>
    /// <typeparam name="T">The type of the object to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The object to cache</param>
    /// <param name="expirationSeconds">Optional expiration time in seconds (uses default if not specified)</param>
    public async Task SetAsync<T>(string key, T value, int? expirationSeconds = null) where T : class
    {
        if (!_isEnabled || _cache == null)
        {
            return;
        }

        try
        {
            var serializedData = JsonSerializer.Serialize(value);
            var expiration = expirationSeconds ?? _options.DefaultExpirationSeconds;

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiration)
            };

            await _cache.SetStringAsync(key, serializedData, cacheOptions);
            _logger.LogDebug("Cached data for key: {Key} (expires in {Expiration}s)", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Remove a cached value
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    public async Task RemoveAsync(string key)
    {
        if (!_isEnabled || _cache == null)
        {
            return;
        }

        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Remove all cached values matching a pattern
    /// Note: This is a simplified implementation. For production with many keys,
    /// consider using Redis SCAN command through StackExchange.Redis directly
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys against</param>
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!_isEnabled || _cache == null)
        {
            return;
        }

        try
        {
            _logger.LogWarning("Pattern-based cache removal is not fully supported with IDistributedCache. Pattern: {Pattern}", pattern);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Check if Redis is available
    /// </summary>
    /// <returns>True if Redis caching is enabled and configured, false otherwise</returns>
    public bool IsAvailable()
    {
        return _isEnabled;
    }
}
