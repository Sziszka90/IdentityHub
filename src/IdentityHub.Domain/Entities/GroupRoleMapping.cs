namespace IdentityHub.Domain.Entities;

/// <summary>
/// Maps an Azure AD group name/id to an application role.
/// </summary>
public class GroupRoleMapping
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Azure AD group name or ID (e.g. "IdentityHub-Admins").
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the mapped role.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The role where the group is mapped
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
