namespace IdentityHub.Contracts.DTOs.AuthorizationConfig.Requests;

public class UpdateRoleRequest
{
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}
