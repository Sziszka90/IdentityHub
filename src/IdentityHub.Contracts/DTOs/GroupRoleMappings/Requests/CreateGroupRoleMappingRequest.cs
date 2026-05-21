namespace IdentityHub.Contracts.DTOs.GroupRoleMappings.Requests;

/// <summary>
/// DTO for creating a group mapping.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class CreateGroupRoleMappingRequest
{
    [Required]
    public string GroupId { get; set; } = string.Empty;

    [Required]
    public string RoleId { get; set; } = string.Empty;
}
