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
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_isEnabled || _cache == null)
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
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!_isEnabled || _cache == null)
        {
            return;
        }

        try
        {
            // This is a limitation of IDistributedCache interface
            // For pattern-based deletion, you would need to use StackExchange.Redis directly
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
    public bool IsAvailable()
    {
        return _isEnabled;
    }
}
