namespace IdentityHub.Domain.Entities;

/// <summary>
/// Many-to-many join between Role and Permission.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Permission
    /// </summary>
    public Permission Permission { get; set; } = null!;
}
