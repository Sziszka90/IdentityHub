using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityHub.Contracts.DTOs.Admin;

namespace IdentityHub.Client;

/// <summary>
/// Typed HTTP client that calls the central IdentityHub.API to retrieve authorization config
/// and perform per-user authorization and identity lookups.
/// Registered in DI via <see cref="IdentityHubClientExtensions.AddIdentityHubClient"/>.
/// The role-permissions and group-mapping snapshots are cached independently for
/// <see cref="IdentityHubClientOptions.CacheSeconds"/> seconds.
/// </summary>
public class IdentityHubClient : HttpClient, IIdentityHubClient
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IdentityHubClientOptions _options;
    private readonly ILogger<IdentityHubClient> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Dictionary<string, List<string>>? _rolePermissions;
    private DateTimeOffset _rolePermissionsFetchedAt = DateTimeOffset.MinValue;

    private Dictionary<string, string>? _groupMapping;
    private DateTimeOffset _groupMappingFetchedAt = DateTimeOffset.MinValue;

    public IdentityHubClient(
        IOptions<IdentityHubClientOptions> options,
        ILogger<IdentityHubClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    // -------------------------------------------------------------------------
    // Authorization config (cached, M2M)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<string>>> GetRolePermissionsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_rolePermissions is not null &&
                DateTimeOffset.UtcNow - _rolePermissionsFetchedAt < TimeSpan.FromSeconds(_options.CacheSeconds))
            {
                return _rolePermissions;
            }

            _logger.LogDebug("Fetching role-permissions from IdentityHub API");

            var response = await GetAsync("api/admin/roles", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            var envelope = JsonSerializer.Deserialize<RolesEnvelopeDto>(body, _json)
                ?? throw new InvalidOperationException("Empty response from /api/admin/roles");

            _rolePermissions = envelope.Roles
                .ToDictionary(
                    r => r.Name,
                    r => r.RolePermissions
                            .Select(rp => rp.Permission.Name)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList());

            _rolePermissionsFetchedAt = DateTimeOffset.UtcNow;
            return _rolePermissions;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetGroupToRoleMappingAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_groupMapping is not null &&
                DateTimeOffset.UtcNow - _groupMappingFetchedAt < TimeSpan.FromSeconds(_options.CacheSeconds))
            {
                return _groupMapping;
            }

            _logger.LogDebug("Fetching group-role mapping from IdentityHub API");

            var response = await GetAsync("api/admin/group-role-mappings", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            var envelope = JsonSerializer.Deserialize<GroupMappingsEnvelopeDto>(body, _json)
                ?? throw new InvalidOperationException("Empty response from /api/admin/group-role-mappings");

            _groupMapping = envelope.GroupRoleMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupName) && m.Role is not null)
                .ToDictionary(m => m.GroupName, m => m.Role!.Name);

            _groupMappingFetchedAt = DateTimeOffset.UtcNow;
            return _groupMapping;
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
    public async Task<PermissionCheckDto> CheckPermissionAsync(
        string permission,
        string bearerToken,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/authorization/check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { permission }, _json),
            Encoding.UTF8,
            "application/json");

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PermissionCheckDto>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/authorization/check");
    }

    // -------------------------------------------------------------------------
    // IdentityController  (GET /api/identity/...)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<UserContextDto> GetCurrentUserAsync(string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<UserContextDto>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/identity/me");
    }

    /// <inheritdoc/>
    public async Task<AuthStatusDto> GetAuthStatusAsync(string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<AuthStatusDto>(body, _json)
            ?? throw new InvalidOperationException("Empty response from /api/identity/status");
    }
}
