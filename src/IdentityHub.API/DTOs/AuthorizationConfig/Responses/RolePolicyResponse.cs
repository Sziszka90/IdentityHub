namespace IdentityHub.API.DTOs.AuthorizationConfig.Responses;

public class RolePolicyResponse
{
    public int Id { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
