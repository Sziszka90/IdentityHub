namespace IdentityHub.Domain.Entities;

/// <summary>
/// Many-to-many join between Role and Permission.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// ID of the Role
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// ID of the Permission
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Permission
    /// </summary>
    public Permission Permission { get; set; } = null!;
}
