using System.Text.Json.Serialization;

namespace IdentityHub.Contracts.DTOs.Admin;

public class RolesEnvelopeDto
{
    [JsonPropertyName("roles")]
    public List<RoleItemDto> Roles { get; set; } = [];
}

public class RoleItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rolePermissions")]
    public List<RolePermissionItemDto> RolePermissions { get; set; } = [];
}

public class RolePermissionItemDto
{
    [JsonPropertyName("permission")]
    public PermissionItemDto Permission { get; set; } = new();
}

public class PermissionItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
