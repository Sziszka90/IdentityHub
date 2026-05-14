namespace IdentityHub.API.DTOs.Groups
{
    /// <summary>
    /// DTO for updating a group mapping.
    /// </summary>
    public class UpdateGroupRequest
    {
        public string RoleId { get; set; } = string.Empty;
    }
}
