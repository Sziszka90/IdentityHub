namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration entry describing a group-to-role mapping to seed.
/// </summary>
public class SeedGroupRoleMappingOptions
{
    public Guid GroupId { get; set; }

    public Guid RoleId { get; set; }
}
