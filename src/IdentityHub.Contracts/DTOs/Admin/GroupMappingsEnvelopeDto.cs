using System.Text.Json.Serialization;

namespace IdentityHub.Contracts.DTOs.Admin;

public class GroupMappingsEnvelopeDto
{
    [JsonPropertyName("groupRoleMappings")]
    public List<GroupMappingItemDto> GroupRoleMappings { get; set; } = [];
}

public class GroupMappingItemDto
{
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public RoleNameItemDto? Role { get; set; }
}

public class RoleNameItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
