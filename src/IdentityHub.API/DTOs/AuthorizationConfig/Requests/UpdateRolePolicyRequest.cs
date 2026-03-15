namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class UpdateRolePolicyRequest
{
    public List<string> RequiredRoles { get; set; } = new();
}
