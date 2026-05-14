namespace IdentityHub.Domain.Entities;

/// <summary>
/// Represents a permission string (e.g., "users.*", "tickets.view.all").
/// </summary>
public class Permission
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Permission name/pattern (e.g. "users.*", "tickets.create").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the permission.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Permissions of the role
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
