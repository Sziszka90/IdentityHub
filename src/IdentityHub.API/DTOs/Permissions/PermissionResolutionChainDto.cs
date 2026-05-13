using IdentityHub.API.DTOs.Groups;

namespace IdentityHub.API.DTOs.Permissions;

/// <summary>
/// Represents the full resolution chain for a user's permissions
/// </summary>
public class PermissionResolutionChainDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<GroupResolution> GroupResolutions { get; set; } = [];
    public List<string> EffectiveRoles { get; set; } = [];
    public List<string> EffectivePermissions { get; set; } = [];
}
