using IdentityHub.Contracts.DTOs.Groups.Responses;
using IdentityHub.Contracts.DTOs.Roles.Responses;

namespace IdentityHub.Contracts.DTOs.GroupRoleMappings.Responses;

public class GroupRoleMappingResponse
{
    public Guid Id { get; set; }
    public GroupResponse Group { get; set; } = new();
    public RoleResponse Role { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
