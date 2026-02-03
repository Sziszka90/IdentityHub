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

    /// <summary>
    /// Context-aware policies with complex conditions
    /// Key: Policy name, Value: Policy configuration
    /// </summary>
    public Dictionary<string, ContextAwarePolicyConfig> ContextPolicies { get; set; } = new();
}

/// <summary>
/// Configuration for a context-aware policy
/// </summary>
public class ContextAwarePolicyConfig
{
    /// <summary>
    /// Required permissions (at least one must match)
    /// </summary>
    public List<string> RequirePermissions { get; set; } = new();

    /// <summary>
    /// Required roles (at least one must match)
    /// </summary>
    public List<string> RequireRoles { get; set; } = new();

    /// <summary>
    /// Whether tenant context is required
    /// </summary>
    public bool RequireTenant { get; set; }

    /// <summary>
    /// Allowed hours (24-hour format). Example: [9, 17] = 9 AM to 5 PM
    /// </summary>
    public TimeRestriction? TimeRestriction { get; set; }

    /// <summary>
    /// Whether MFA is required for this policy
    /// </summary>
    public bool RequireMfa { get; set; }

    /// <summary>
    /// Specific tenant IDs allowed (empty = all tenants)
    /// </summary>
    public List<string> AllowedTenants { get; set; } = new();

    /// <summary>
    /// Custom claim requirements
    /// Key: Claim type, Value: Required claim value
    /// </summary>
    public Dictionary<string, string> RequireCustomClaims { get; set; } = new();
}

/// <summary>
/// Time-based access restrictions
/// </summary>
public class TimeRestriction
{
    /// <summary>
    /// Start hour (0-23, 24-hour format)
    /// </summary>
    public int StartHour { get; set; }

    /// <summary>
    /// End hour (0-23, 24-hour format)
    /// </summary>
    public int EndHour { get; set; }

    /// <summary>
    /// Allowed days of week (1=Monday, 7=Sunday). Empty = all days
    /// </summary>
    public List<int> AllowedDays { get; set; } = new();

    /// <summary>
    /// Timezone for time checks (default: UTC)
    /// </summary>
    public string Timezone { get; set; } = "UTC";
}
