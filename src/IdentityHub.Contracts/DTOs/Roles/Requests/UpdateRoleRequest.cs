namespace IdentityHub.Contracts.DTOs.Roles.Requests;

using System.ComponentModel.DataAnnotations;

public class UpdateRoleRequest
{
    [StringLength(500)]
    public string? Description { get; set; }

    [MinLength(1)]
    public List<string> Permissions { get; set; } = new();
}
