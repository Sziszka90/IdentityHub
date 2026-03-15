namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class CreateGroupRoleMappingRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
