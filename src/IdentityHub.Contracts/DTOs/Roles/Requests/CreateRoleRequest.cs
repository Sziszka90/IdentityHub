namespace IdentityHub.Contracts.DTOs.Roles.Requests;

using System.ComponentModel.DataAnnotations;

public class CreateRoleRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [MinLength(1)]
    public List<string> Permissions { get; set; } = new();
}
