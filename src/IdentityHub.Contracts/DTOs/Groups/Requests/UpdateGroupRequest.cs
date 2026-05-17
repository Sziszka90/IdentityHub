namespace IdentityHub.Contracts.DTOs.Groups.Requests;

/// <summary>
/// DTO for updating a group mapping.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class UpdateGroupRequest
{
    [Required]
    public string RoleId { get; set; } = string.Empty;
}
