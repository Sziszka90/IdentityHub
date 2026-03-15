namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class CreateRolePolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = new();
}
