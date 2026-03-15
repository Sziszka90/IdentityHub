namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class CreatePermissionPolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string RequiredPermission { get; set; } = string.Empty;
}
