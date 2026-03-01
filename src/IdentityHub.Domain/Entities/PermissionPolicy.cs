namespace IdentityHub.Domain.Entities;

/// <summary>
/// A named authorization policy that requires a specific permission.
/// Example: "CanManageUsers" → "users.manage"
/// </summary>
public class PermissionPolicy
{
    public int Id { get; set; }

    /// <summary>
    /// Policy name used in [Authorize(Policy = "...")] attributes.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// The permission string required by this policy.
    /// </summary>
    public string RequiredPermission { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
