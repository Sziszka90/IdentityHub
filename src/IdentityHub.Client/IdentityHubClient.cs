using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityHub.Client.Caching;
using IdentityHub.Contracts.DTOs.Admin;
using IdentityHub.Contracts.DTOs.Identity.Responses;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.Client;

/// <summary>
/// Typed HTTP client that calls the central IdentityHub.API to retrieve authorization config
/// and perform per-user authorization and identity lookups.
/// Registered in DI via <see cref="IdentityHubClientExtensions.AddIdentityHubClient"/>.
/// Cached responses are stored through the configured cache backend.
/// </summary>
public class IdentityHubClient : HttpClient, IIdentityHubClient
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IdentityHubClientOptions _options;
    private readonly IIdentityHubCacheStore _cacheStore;
    private readonly ILogger<IdentityHubClient> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public IdentityHubClient(
        IOptions<IdentityHubClientOptions> options,
        IIdentityHubCacheStore cacheStore,
        ILogger<IdentityHubClient> logger)
    {
        _options = options.Value;
        _cacheStore = cacheStore;
        _logger = logger;

        BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    // -------------------------------------------------------------------------
    // Authorization config (cached, M2M)
    // -------------------------------------------------------------------------

    private string RolePermissionsCacheKey => $"{_options.CacheKeyPrefix}:role-permissions";

    private string GroupMappingCacheKey => $"{_options.CacheKeyPrefix}:group-role-mappings";

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<string>>> GetRolePermissionsAsync(CancellationToken ct = default)
    {
        var cached = await _cacheStore.GetAsync<Dictionary<string, List<string>>>(RolePermissionsCacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        await _lock.WaitAsync(ct);
        try
        {
            cached = await _cacheStore.GetAsync<Dictionary<string, List<string>>>(RolePermissionsCacheKey, ct);
            if (cached is not null)
            {
                return cached;
            }

            _logger.LogDebug("Fetching role-permissions from IdentityHub API");

            var response = await GetAsync("api/admin/roles", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            var envelope = JsonSerializer.Deserialize<RolesEnvelopeDto>(body, _json)
                ?? throw new InvalidOperationException("Empty response from /api/admin/roles");

            var rolePermissions = envelope.Roles
                .ToDictionary(
                    r => r.Name,
                    r => r.RolePermissions
                            .Select(rp => rp.Permission.Name)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList());

            await _cacheStore.SetAsync(
                RolePermissionsCacheKey,
                rolePermissions,
                TimeSpan.FromSeconds(_options.CacheSeconds),
                ct);

            return rolePermissions;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetGroupToRoleMappingAsync(CancellationToken ct = default)
    {
        var cached = await _cacheStore.GetAsync<Dictionary<string, string>>(GroupMappingCacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        await _lock.WaitAsync(ct);
        try
        {
            cached = await _cacheStore.GetAsync<Dictionary<string, string>>(GroupMappingCacheKey, ct);
            if (cached is not null)
            {
                return cached;
            }

            _logger.LogDebug("Fetching group-role mapping from IdentityHub API");

            var response = await GetAsync("api/admin/group-role-mappings", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            var envelope = JsonSerializer.Deserialize<GroupMappingsEnvelopeDto>(body, _json)
                ?? throw new InvalidOperationException("Empty response from /api/admin/group-role-mappings");

            var groupMapping = envelope.GroupRoleMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupName) && m.Role is not null)
                .ToDictionary(m => m.GroupName, m => m.Role!.Name);

            await _cacheStore.SetAsync(
                GroupMappingCacheKey,
                groupMapping,
                TimeSpan.FromSeconds(_options.CacheSeconds),
                ct);

            return groupMapping;
        }
        finally
        {
            _lock.Release();
        }
    }

    // -------------------------------------------------------------------------
    // AuthorizationController  (POST /api/authorization/check)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<PermissionCheckResponse> CheckPermissionAsync(
        string permission,
        string bearerToken,
        CancellationToken ct = default)
    {
        var cacheKey = BuildPermissionCheckCacheKey(permission, bearerToken);
        var cached = await _cacheStore.GetAsync<PermissionCheckResponse>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/authorization/check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { permission }, _json),
            Encoding.UTF8,
            "application/json");

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var permissionCheck = JsonSerializer.Deserialize<PermissionCheckResponse>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/authorization/check");

        await _cacheStore.SetAsync(
            cacheKey,
            permissionCheck,
            TimeSpan.FromSeconds(_options.PermissionCheckCacheSeconds),
            ct);

        return permissionCheck;
    }

    // -------------------------------------------------------------------------
    // IdentityController  (GET /api/identity/...)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<UserContextResponse> GetCurrentUserAsync(string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<UserContextResponse>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/identity/me");
    }

    /// <inheritdoc/>
    public async Task<AuthStatusResponse> GetAuthStatusAsync(string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<AuthStatusResponse>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/identity/status");
    }

    private string BuildPermissionCheckCacheKey(string permission, string bearerToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bearerToken)));
        return $"{_options.CacheKeyPrefix}:permission:{permission}:{tokenHash}";
    }
}
