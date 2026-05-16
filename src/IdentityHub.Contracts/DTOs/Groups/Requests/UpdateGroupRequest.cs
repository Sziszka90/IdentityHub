namespace IdentityHub.Contracts.DTOs.Groups.Requests;

/// <summary>
/// DTO for updating a group mapping.
/// </summary>
public class UpdateGroupRequest
{
    public string RoleId { get; set; } = string.Empty;
}
