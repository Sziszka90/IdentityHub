namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration for authorization policies
/// </summary>
public class AuthorizationPoliciesOptions
{
    public const string SectionName = "AuthorizationPolicies";

    /// <summary>
    /// Permission-based policies
    /// Key: Policy name, Value: Required permission
    /// </summary>
    public Dictionary<string, string> PermissionPolicies { get; set; } = new();

    /// <summary>
    /// Role-based policies
    /// Key: Policy name, Value: Required roles (comma-separated)
    /// </summary>
    public Dictionary<string, string> RolePolicies { get; set; } = new();
}
