namespace IdentityHub.Contracts.DTOs.Roles.Responses;

public class GroupRoleMappingResponse
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
