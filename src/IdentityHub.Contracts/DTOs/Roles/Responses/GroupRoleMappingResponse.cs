using IdentityHub.Contracts.DTOs.Groups.Responses;

namespace IdentityHub.Contracts.DTOs.Roles.Responses;

public class GroupRoleMappingResponse
{
    public Guid Id { get; set; }
    public GroupResponse Group { get; set; } = new();
    public RoleResponse Role { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
