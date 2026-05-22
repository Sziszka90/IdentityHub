using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Graph.Models;

namespace IdentityHub.Domain.Entities;

/// <summary>
/// Maps an Azure AD group name/id to an application role.
/// </summary>
public class GroupRoleMapping : ITenantOwnedEntity
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Azure AD group name or ID (e.g. "IdentityHub-Admins").
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Tenant that owns this mapping.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The group where the role is mapped (populated from Graph, not persisted in the database).
    /// </summary>
    [NotMapped]
    public Group? Group { get; set; }

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
