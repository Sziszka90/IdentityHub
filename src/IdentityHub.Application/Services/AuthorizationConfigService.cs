using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing authorization configuration.
/// Encapsulates business logic for roles, group-role mappings, permission policies, and role policies.
/// </summary>
public class AuthorizationConfigService : IAuthorizationConfigService
{
    private readonly IPermissionPoliciesRepository _permissionPoliciesRepository;
    private readonly IPermissionsRepository _permissionsRepository;
    private readonly IRolePoliciesRepository _rolePoliciesRepository;
    private readonly IRolesRepository _roleRepository;
    private readonly ILogger<AuthorizationConfigService> _logger;

    public AuthorizationConfigService(
        IAuthorizationConfigRepository repo,
        ILogger<AuthorizationConfigService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<Dictionary<string, List<string>>> GetAllRolePermissionsAsync(CancellationToken ct = default)
        => _repo.GetAllRolePermissionsAsync(ct);

    public Task<Dictionary<string, string>> GetGroupToRoleDictionaryAsync(CancellationToken ct = default)
        => _repo.GetGroupToRoleDictionaryAsync(ct);

    // ── Roles ─────────────────────────────────────────────────────────────

    public Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => _repo.GetAllRolesAsync(ct);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => _repo.GetRoleByNameAsync(name, ct);

    public async Task<Role?> CreateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        if (await _repo.GetRoleByNameAsync(name, ct) is not null)
            return null;

        var role = await _repo.CreateRoleAsync(new Role { Name = name, Description = description }, ct);

        if (permissions.Count > 0)
            await _repo.SetRolePermissionsAsync(role.Name, permissions, ct);

        return await _repo.GetRoleByNameAsync(role.Name, ct);
    }

    public async Task<Role?> UpdateRoleAsync(string name, string? description, List<string> permissions, CancellationToken ct = default)
    {
        var role = await _repo.GetRoleByNameAsync(name, ct);
        if (role is null)
            return null;

        role.Description = description;
        await _repo.UpdateRoleAsync(role, ct);
        await _repo.SetRolePermissionsAsync(name, permissions, ct);

        return await _repo.GetRoleByNameAsync(name, ct);
    }

    public async Task<bool> DeleteRoleAsync(string name, CancellationToken ct = default)
    {
        var role = await _repo.GetRoleByNameAsync(name, ct);
        if (role is null)
            return false;

        return await _repo.DeleteRoleAsync(role.Id, ct);
    }

    // ── Group-Role Mappings ───────────────────────────────────────────────

    public Task<List<GroupRoleMapping>> GetAllGroupMappingsAsync(CancellationToken ct = default)
        => _repo.GetAllGroupRoleMappingsAsync(ct);

    public Task<GroupRoleMapping?> GetGroupMappingByGroupNameAsync(string groupName, CancellationToken ct = default)
        => _repo.GetGroupRoleMappingByGroupNameAsync(groupName, ct);

    public async Task<GroupRoleMapping?> CreateGroupMappingAsync(string groupName, int roleId, CancellationToken ct = default)
    {
        if (await _repo.GetGroupRoleMappingByGroupNameAsync(groupName, ct) is not null)
            return null;

        return await _repo.CreateGroupRoleMappingAsync(
            new GroupRoleMapping { GroupName = groupName, RoleId = roleId }, ct);
    }

    public async Task<GroupRoleMapping?> UpdateGroupMappingAsync(int id, int roleId, CancellationToken ct = default)
    {
        var mappings = await _repo.GetAllGroupRoleMappingsAsync(ct);
        var mapping = mappings.FirstOrDefault(m => m.Id == id);
        if (mapping is null)
            return null;

        var tracked = await _repo.GetGroupRoleMappingByGroupNameAsync(mapping.GroupName, ct);
        tracked!.RoleId = roleId;
        return await _repo.UpdateGroupRoleMappingAsync(tracked, ct);
    }

    public Task<bool> DeleteGroupMappingAsync(int id, CancellationToken ct = default)
        => _repo.DeleteGroupRoleMappingAsync(id, ct);

    // ── Permission Policies ───────────────────────────────────────────────

    public Task<List<PermissionPolicy>> GetAllPermissionPoliciesAsync(CancellationToken ct = default)
        => _repo.GetAllPermissionPoliciesAsync(ct);

    public async Task<PermissionPolicy?> CreatePermissionPolicyAsync(string policyName, string requiredPermission, CancellationToken ct = default)
    {
        if (await _repo.GetPermissionPolicyByNameAsync(policyName, ct) is not null)
            return null;

        return await _repo.CreatePermissionPolicyAsync(
            new PermissionPolicy { PolicyName = policyName, RequiredPermission = requiredPermission }, ct);
    }

    public async Task<PermissionPolicy?> UpdatePermissionPolicyAsync(int id, string requiredPermission, CancellationToken ct = default)
    {
        var policies = await _repo.GetAllPermissionPoliciesAsync(ct);
        var policy = policies.FirstOrDefault(p => p.Id == id);
        if (policy is null)
            return null;

        var tracked = (await _repo.GetPermissionPolicyByNameAsync(policy.PolicyName, ct))!;
        tracked.RequiredPermission = requiredPermission;
        return await _repo.UpdatePermissionPolicyAsync(tracked, ct);
    }

    public Task<bool> DeletePermissionPolicyAsync(int id, CancellationToken ct = default)
        => _repo.DeletePermissionPolicyAsync(id, ct);

    // ── Role Policies ─────────────────────────────────────────────────────

    public Task<List<RolePolicy>> GetAllRolePoliciesAsync(CancellationToken ct = default)
        => _repo.GetAllRolePoliciesAsync(ct);

    public async Task<RolePolicy?> CreateRolePolicyAsync(string policyName, List<string> requiredRoles, CancellationToken ct = default)
    {
        if (await _repo.GetRolePolicyByNameAsync(policyName, ct) is not null)
            return null;

        return await _repo.CreateRolePolicyAsync(new RolePolicy
        {
            PolicyName = policyName,
            RequiredRoles = string.Join(",", requiredRoles)
        }, ct);
    }

    public async Task<RolePolicy?> UpdateRolePolicyAsync(int id, List<string> requiredRoles, CancellationToken ct = default)
    {
        var policies = await _repo.GetAllRolePoliciesAsync(ct);
        var policy = policies.FirstOrDefault(p => p.Id == id);
        if (policy is null)
            return null;

        var tracked = (await _repo.GetRolePolicyByNameAsync(policy.PolicyName, ct))!;
        tracked.RequiredRoles = string.Join(",", requiredRoles);
        return await _repo.UpdateRolePolicyAsync(tracked, ct);
    }

    public Task<bool> DeleteRolePolicyAsync(int id, CancellationToken ct = default)
        => _repo.DeleteRolePolicyAsync(id, ct);
}
