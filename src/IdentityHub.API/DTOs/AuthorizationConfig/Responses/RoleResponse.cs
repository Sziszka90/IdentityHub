namespace IdentityHub.API.DTOs.AuthorizationConfig.Responses;

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
