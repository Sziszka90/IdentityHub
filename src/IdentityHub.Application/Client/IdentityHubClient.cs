using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Client;

/// <summary>
/// Typed HTTP client that calls the central IdentityHub.API to retrieve authorization config.
/// Registered in DI via <see cref="IdentityHubClientExtensions.AddIdentityHubClient"/>.
/// Each data slice is cached independently for <see cref="IdentityHubClientOptions.CacheSeconds"/> seconds.
/// </summary>
public class IdentityHubClient : HttpClient, IIdentityHubClient
{
    private readonly IdentityHubClientOptions _options;
    private readonly ILogger<IdentityHubClient> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Dictionary<string, List<string>>? _rolePermissions;
    private DateTimeOffset _rolePermissionsFetchedAt = DateTimeOffset.MinValue;

    private Dictionary<string, string>? _groupMapping;
    private DateTimeOffset _groupMappingFetchedAt = DateTimeOffset.MinValue;

    private Dictionary<string, string>? _permissionPolicies;
    private DateTimeOffset _permissionPoliciesFetchedAt = DateTimeOffset.MinValue;

    private Dictionary<string, string>? _rolePolicies;
    private DateTimeOffset _rolePoliciesFetchedAt = DateTimeOffset.MinValue;

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

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<string>>> GetRolePermissionsAsync(CancellationToken ct = default)
    {
        if (_rolePermissions is not null && !IsExpired(_rolePermissionsFetchedAt))
            return _rolePermissions;

        await _lock.WaitAsync(ct);
        try
        {
            if (_rolePermissions is not null && !IsExpired(_rolePermissionsFetchedAt))
                return _rolePermissions;

            _logger.LogInformation("Fetching role-permissions from IdentityHub.API");
            var roles = await this.GetFromJsonAsync<List<RoleApiResponse>>(
                "api/authorization-config/roles", ct)
                ?? throw new InvalidOperationException("IdentityHub.API returned null for role-permissions.");

            _rolePermissions = roles.ToDictionary(r => r.Name, r => r.Permissions);
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
        if (_groupMapping is not null && !IsExpired(_groupMappingFetchedAt))
            return _groupMapping;

        await _lock.WaitAsync(ct);
        try
        {
            if (_groupMapping is not null && !IsExpired(_groupMappingFetchedAt))
                return _groupMapping;

            _logger.LogInformation("Fetching group-role mappings from IdentityHub.API");
            var mappings = await this.GetFromJsonAsync<List<GroupMappingApiResponse>>(
                "api/authorization-config/group-mappings", ct)
                ?? throw new InvalidOperationException("IdentityHub.API returned null for group-role mappings.");

            _groupMapping = mappings.ToDictionary(m => m.GroupName, m => m.RoleName);
            _groupMappingFetchedAt = DateTimeOffset.UtcNow;
            return _groupMapping;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetPermissionPoliciesAsync(CancellationToken ct = default)
    {
        if (_permissionPolicies is not null && !IsExpired(_permissionPoliciesFetchedAt))
            return _permissionPolicies;

        await _lock.WaitAsync(ct);
        try
        {
            if (_permissionPolicies is not null && !IsExpired(_permissionPoliciesFetchedAt))
                return _permissionPolicies;

            _logger.LogInformation("Fetching permission-policies from IdentityHub.API");
            var policies = await this.GetFromJsonAsync<List<PermissionPolicyApiResponse>>(
                "api/authorization-config/permission-policies", ct)
                ?? throw new InvalidOperationException("IdentityHub.API returned null for permission-policies.");

            _permissionPolicies = policies.ToDictionary(p => p.PolicyName, p => p.RequiredPermission);
            _permissionPoliciesFetchedAt = DateTimeOffset.UtcNow;
            return _permissionPolicies;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetRolePoliciesAsync(CancellationToken ct = default)
    {
        if (_rolePolicies is not null && !IsExpired(_rolePoliciesFetchedAt))
            return _rolePolicies;

        await _lock.WaitAsync(ct);
        try
        {
            if (_rolePolicies is not null && !IsExpired(_rolePoliciesFetchedAt))
                return _rolePolicies;

            _logger.LogInformation("Fetching role-policies from IdentityHub.API");
            var policies = await this.GetFromJsonAsync<List<RolePolicyApiResponse>>(
                "api/authorization-config/role-policies", ct)
                ?? throw new InvalidOperationException("IdentityHub.API returned null for role-policies.");

            // Serialise List<string> → comma-separated to stay consistent with
            // how AuthorizationExtensions.AddAuthorizationPolicies consumes this map.
            _rolePolicies = policies.ToDictionary(
                p => p.PolicyName,
                p => string.Join(",", p.RequiredRoles));
            _rolePoliciesFetchedAt = DateTimeOffset.UtcNow;
            return _rolePolicies;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsExpired(DateTimeOffset fetchedAt)
        => (DateTimeOffset.UtcNow - fetchedAt).TotalSeconds >= _options.CacheSeconds;

    // ── Private response models matching the API's JSON output ────────────────

    private sealed class RoleApiResponse
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = [];
    }

    private sealed class GroupMappingApiResponse
    {
        public string GroupName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    private sealed class PermissionPolicyApiResponse
    {
        public string PolicyName { get; set; } = string.Empty;
        public string RequiredPermission { get; set; } = string.Empty;
    }

    private sealed class RolePolicyApiResponse
    {
        public string PolicyName { get; set; } = string.Empty;
        public List<string> RequiredRoles { get; set; } = [];
    }
}

