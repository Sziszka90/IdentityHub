namespace IdentityHub.API.DTOs.AuthorizationConfig.Requests;

public class AssignRolesRequest
{
    /// <summary>
    /// List of role names to assign or remove.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
