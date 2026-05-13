namespace IdentityHub.Domain.Entities;

/// <summary>
/// Represents an application role (e.g., Admin, SupportAgent, Developer).
/// </summary>
public class Role
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique role name (e.g. "Admin", "SupportAgent").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Permissions of the role
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    /// <summary>
    /// Group role mappings
    /// </summary>
    public ICollection<GroupRoleMapping> GroupRoleMappings { get; set; } = [];
}
