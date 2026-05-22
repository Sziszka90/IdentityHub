using Microsoft.Extensions.Caching.Memory;

namespace IdentityHub.Client.Caching;

/// <summary>
/// In-process memory cache implementation for IdentityHub client data.
/// </summary>
public class MemoryIdentityHubCacheStore : IIdentityHubCacheStore
{
    private readonly IMemoryCache _memoryCache;

    public MemoryIdentityHubCacheStore(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_memoryCache.TryGetValue(key, out T? value) ? value : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _memoryCache.Set(key, value, ttl);
        return Task.CompletedTask;
    }
}
