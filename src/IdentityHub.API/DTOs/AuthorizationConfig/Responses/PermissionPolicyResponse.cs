namespace IdentityHub.API.DTOs.AuthorizationConfig.Responses;

public class PermissionPolicyResponse
{
    public int Id { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string RequiredPermission { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
