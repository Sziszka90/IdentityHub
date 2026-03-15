namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class UpdatePermissionPolicyRequest
{
    public string RequiredPermission { get; set; } = string.Empty;
}
