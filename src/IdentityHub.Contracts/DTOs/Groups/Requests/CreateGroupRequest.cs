namespace IdentityHub.Contracts.DTOs.Groups.Requests;

/// <summary>
/// DTO for creating a group mapping.
/// </summary>
public class CreateGroupRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}
