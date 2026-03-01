namespace IdentityHub.Domain.Entities;

/// <summary>
/// A named authorization policy that requires one or more roles.
/// Example: "RequireAdmin" → "Admin" or "RequireAdminOrAgent" → "Admin,SupportAgent"
/// </summary>
public class RolePolicy
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Policy name used in [Authorize(Policy = "...")] attributes.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of required roles (at least one must match).
    /// </summary>
    public string RequiredRoles { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
