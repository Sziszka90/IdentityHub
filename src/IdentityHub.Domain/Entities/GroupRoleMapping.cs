namespace IdentityHub.Domain.Entities;

/// <summary>
/// Maps an Azure AD group name/id to an application role.
/// </summary>
public class GroupRoleMapping
{
    public int Id { get; set; }

    /// <summary>
    /// Azure AD group name or ID (e.g. "IdentityHub-Admins").
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the mapped role.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// The role where the group is mapped
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
