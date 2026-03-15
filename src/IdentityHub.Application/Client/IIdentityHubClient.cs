namespace IdentityHub.Application.Client;

/// <summary>
/// Abstraction over the HTTP client that fetches authorization config from the central IdentityHub.API.
/// </summary>
public interface IIdentityHubClient
{
    /// <summary>Returns the role → permissions mapping (roleName → list of permission names).</summary>
    Task<Dictionary<string, List<string>>> GetRolePermissionsAsync(CancellationToken ct = default);

    /// <summary>Returns the group → role mapping (groupName → roleName).</summary>
    Task<Dictionary<string, string>> GetGroupToRoleMappingAsync(CancellationToken ct = default);

    /// <summary>Returns the permission policy → required permission mapping (policyName → permission).</summary>
    Task<Dictionary<string, string>> GetPermissionPoliciesAsync(CancellationToken ct = default);

    /// <summary>Returns the role policy → required role mapping (policyName → role).</summary>
    Task<Dictionary<string, string>> GetRolePoliciesAsync(CancellationToken ct = default);
}
