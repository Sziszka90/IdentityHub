namespace IdentityHub.API.DTOs.Groups.Requests;

/// <summary>
/// Shows how a group maps to roles and permissions
/// </summary>
public class GroupResolution
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string? MappedRole { get; set; }
    public List<string> Permissions { get; set; } = [];
}
