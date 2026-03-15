namespace IdentityHub.API.DTOs.AuthorizationConfig.Responses;

public class AuthorizationConfigResponse
{
    public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
    public Dictionary<string, string> GroupToRoleMapping { get; set; } = new();
    public Dictionary<string, string> PermissionPolicies { get; set; } = new();
    public Dictionary<string, string> RolePolicies { get; set; } = new();
}
