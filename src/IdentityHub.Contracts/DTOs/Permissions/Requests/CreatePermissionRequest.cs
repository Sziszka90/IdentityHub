namespace IdentityHub.Contracts.DTOs.Permissions.Requests;

/// <summary>
/// DTO for creating a permission.
/// </summary>
public class CreatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
}
