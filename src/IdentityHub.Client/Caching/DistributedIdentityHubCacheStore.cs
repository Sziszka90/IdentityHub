using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace IdentityHub.Client.Caching;

/// <summary>
/// Distributed cache implementation for IdentityHub client data.
/// </summary>
public class DistributedIdentityHubCacheStore : IIdentityHubCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IDistributedCache _distributedCache;

    public DistributedIdentityHubCacheStore(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _distributedCache.GetStringAsync(key, ct);
        return string.IsNullOrWhiteSpace(value)
            ? default
            : JsonSerializer.Deserialize<T>(value, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var serialized = JsonSerializer.Serialize(value, JsonOptions);
        return _distributedCache.SetStringAsync(
            key,
            serialized,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            },
            ct);
    }
}
