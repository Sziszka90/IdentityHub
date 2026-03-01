namespace IdentityHub.API.DTOs;

// ── Role DTOs ──

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class RoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Group-Role mapping DTOs ──

public class CreateGroupRoleMappingRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class UpdateGroupRoleMappingRequest
{
    public string RoleName { get; set; } = string.Empty;
}

public class GroupRoleMappingResponse
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Permission Policy DTOs ──

public class CreatePermissionPolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string RequiredPermission { get; set; } = string.Empty;
}

public class UpdatePermissionPolicyRequest
{
    public string RequiredPermission { get; set; } = string.Empty;
}

public class PermissionPolicyResponse
{
    public int Id { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string RequiredPermission { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Role Policy DTOs ──

public class CreateRolePolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = new();
}

public class UpdateRolePolicyRequest
{
    public List<string> RequiredRoles { get; set; } = new();
}

public class RolePolicyResponse
{
    public int Id { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Full configuration snapshot ──

public class AuthorizationConfigResponse
{
    public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
    public Dictionary<string, string> GroupToRoleMapping { get; set; } = new();
    public Dictionary<string, string> PermissionPolicies { get; set; } = new();
    public Dictionary<string, string> RolePolicies { get; set; } = new();
}
