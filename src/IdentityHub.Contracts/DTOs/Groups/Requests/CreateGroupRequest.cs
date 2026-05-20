namespace IdentityHub.Contracts.DTOs.Groups.Requests;

/// <summary>
/// DTO for creating a group mapping.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class CreateGroupRequest
{
    [Required]
    public string GroupId { get; set; } = string.Empty;

    [Required]
    public string RoleId { get; set; } = string.Empty;
}
