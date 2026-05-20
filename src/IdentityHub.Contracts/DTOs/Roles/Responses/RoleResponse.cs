using IdentityHub.Contracts.DTOs.Permissions.Responses;

namespace IdentityHub.Contracts.DTOs.Roles.Responses;

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
