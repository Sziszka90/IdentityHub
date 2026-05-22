namespace IdentityHub.Client.Caching;

/// <summary>
/// Cache abstraction used by <see cref="IdentityHubClient"/>.
/// </summary>
public interface IIdentityHubCacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
}
