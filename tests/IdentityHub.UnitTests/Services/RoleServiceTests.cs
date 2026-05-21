using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

public class RoleServiceTests
{
    private readonly Mock<IRolesRepository> _rolesRepoMock = new();
    private readonly Mock<IPermissionsRepository> _permissionsRepoMock = new();
    private readonly Mock<IGraphService> _graphServiceMock = new();
    private readonly Mock<ILogger<RoleService>> _loggerMock = new();

    private RoleService CreateService() =>
        new(_rolesRepoMock.Object, _permissionsRepoMock.Object, _graphServiceMock.Object, _loggerMock.Object);

    // -------------------------------------------------------------------------
    // GetDirectRolesForUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDirectRolesForUserAsync_ReturnsEmpty_WhenUserHasNoGroups()
    {
        _graphServiceMock.Setup(g => g.GetUserDirectGroupIdsAsync("u1")).ReturnsAsync([]);

        var result = await CreateService().GetDirectRolesForUserAsync("u1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDirectRolesForUserAsync_ReturnsRoles_WhenMappingsExist()
    {
        var roleId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Admin" };

        _graphServiceMock.Setup(g => g.GetUserDirectGroupIdsAsync("u1")).ReturnsAsync(["grp-admins"]);
        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingsByGroupIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupRoleMapping { GroupId = Guid.NewGuid(), RoleId = roleId }]);
        _rolesRepoMock.Setup(r => r.GetRolesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([role]);

        var result = await CreateService().GetDirectRolesForUserAsync("u1");

        Assert.Single(result);
        Assert.Equal("Admin", result[0].Name);
    }

    // -------------------------------------------------------------------------
    // GetTransitiveRolesForUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTransitiveRolesForUserAsync_ReturnsEmpty_WhenUserHasNoGroups()
    {
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync([]);

        var result = await CreateService().GetTransitiveRolesForUserAsync("u1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTransitiveRolesForUserAsync_ReturnsRoles_WhenMappingsExist()
    {
        var roleId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Viewer" };

        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync(["grp-viewers"]);
        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingsByGroupIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupRoleMapping { GroupId = Guid.NewGuid(), RoleId = roleId }]);
        _rolesRepoMock.Setup(r => r.GetRolesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([role]);

        var result = await CreateService().GetTransitiveRolesForUserAsync("u1");

        Assert.Single(result);
        Assert.Equal("Viewer", result[0].Name);
    }

    // -------------------------------------------------------------------------
    // CreateRoleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRoleByIdAsync_ReturnsRole_WhenExists()
    {
        var id = Guid.NewGuid();
        var existing = new Role { Id = id, Name = "Admin" };
        _rolesRepoMock.Setup(r => r.GetRoleByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await CreateService().GetRoleByIdAsync(id);

        Assert.Equal(existing, result);
    }

    [Fact]
    public async Task CreateRoleAsync_ReturnsExistingRole_WhenNameAlreadyExists()
    {
        var existing = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        _rolesRepoMock.Setup(r => r.GetRoleByNameAsync("Admin", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await CreateService().CreateRoleAsync("Admin", null, []);

        Assert.Equal(existing, result);
        _rolesRepoMock.Verify(r => r.CreateRoleAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRoleAsync_CreatesRoleAndAssignsPermissions()
    {
        var created = new Role { Id = Guid.NewGuid(), Name = "Editor" };
        _rolesRepoMock.SetupSequence(r => r.GetRoleByNameAsync("Editor", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null)   // first call: not found → proceed to create
            .ReturnsAsync(created);      // second call: return after creation
        _rolesRepoMock.Setup(r => r.CreateRoleAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await CreateService().CreateRoleAsync("Editor", "Edits content", ["articles.write"]);

        Assert.NotNull(result);
        _permissionsRepoMock.Verify(p => p.SetRolePermissionsAsync("Editor", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateRoleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateRoleAsync_ReturnsNull_WhenRoleNotFound()
    {
        var roleId = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await CreateService().UpdateRoleAsync(roleId, "desc", []);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRoleAsync_UpdatesAndReturnsRole()
    {
        var roleId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Admin", Description = "Old" };
        var updated = new Role { Id = role.Id, Name = "Admin", Description = "New" };

        _rolesRepoMock.SetupSequence(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role)
            .ReturnsAsync(updated);
        _rolesRepoMock.Setup(r => r.UpdateRoleAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await CreateService().UpdateRoleAsync(roleId, "New", ["users.read"]);

        Assert.NotNull(result);
        Assert.Equal("New", result!.Description);
        _permissionsRepoMock.Verify(p => p.SetRolePermissionsAsync("Admin", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteRoleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteRoleAsync_ReturnsFalse_WhenRoleNotFound()
    {
        var roleId = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().DeleteRoleAsync(roleId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsTrue_WhenRoleDeleted()
    {
        var id = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.DeleteRoleAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().DeleteRoleAsync(id);

        Assert.True(result);
    }

    // -------------------------------------------------------------------------
    // CreateGroupMappingAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateGroupMappingAsync_ReturnsNull_WhenMappingAlreadyExists()
    {
        var roleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupId = groupId, RoleId = roleId });

        var result = await CreateService().CreateGroupMappingAsync(groupId, roleId);

        Assert.Null(result);
        _rolesRepoMock.Verify(r => r.CreateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupMappingAsync_CreatesAndReturnsMapping()
    {
        var roleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var mapping = new GroupRoleMapping { GroupId = groupId, RoleId = roleId };

        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId, It.IsAny<CancellationToken>())).ReturnsAsync((GroupRoleMapping?)null);
        _rolesRepoMock.Setup(r => r.CreateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>())).ReturnsAsync(mapping);

        var result = await CreateService().CreateGroupMappingAsync(groupId, roleId);

        Assert.NotNull(result);
        Assert.Equal(groupId, result!.GroupId);
    }

    // -------------------------------------------------------------------------
    // UpdateGroupMappingAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateGroupMappingAsync_ReturnsNull_WhenMappingNotFound()
    {
        _rolesRepoMock.Setup(r => r.GetAllGroupRoleMappingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateService().UpdateGroupMappingAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateGroupMappingAsync_UpdatesAndReturnsMapping()
    {
        var id = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var existing = new GroupRoleMapping { Id = id, GroupId = groupId, RoleId = Guid.NewGuid() };
        var updated = new GroupRoleMapping { Id = id, GroupId = groupId, RoleId = newRoleId };

        _rolesRepoMock.Setup(r => r.GetAllGroupRoleMappingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([existing]);
        _rolesRepoMock.Setup(r => r.UpdateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await CreateService().UpdateGroupMappingAsync(id, groupId, newRoleId);

        Assert.NotNull(result);
        Assert.Equal(newRoleId, result!.RoleId);
    }
}
