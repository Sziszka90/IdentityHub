namespace IdentityHub.Contracts.DTOs.Tenants.Responses;

/// <summary>
/// Tenant handling settings exposed by the API for diagnostics.
/// </summary>
public class TenantConfigurationResponse
{
    public string HeaderName { get; init; } = string.Empty;

    public string CurrentTenantId { get; init; } = string.Empty;

    public bool IsCurrentTenantAllowed { get; init; }

    public List<string> AllowedTenantIds { get; init; } = [];
}
