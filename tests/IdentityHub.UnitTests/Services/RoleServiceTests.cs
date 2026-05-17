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
            .ReturnsAsync([new GroupRoleMapping { GroupName = "grp-admins", RoleId = roleId }]);
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
            .ReturnsAsync([new GroupRoleMapping { GroupName = "grp-viewers", RoleId = roleId }]);
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
        _rolesRepoMock.Setup(r => r.GetRoleByNameAsync("NonExistent", It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await CreateService().UpdateRoleAsync("NonExistent", "desc", []);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRoleAsync_UpdatesAndReturnsRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Old" };
        var updated = new Role { Id = role.Id, Name = "Admin", Description = "New" };

        _rolesRepoMock.SetupSequence(r => r.GetRoleByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role)
            .ReturnsAsync(updated);
        _rolesRepoMock.Setup(r => r.UpdateRoleAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await CreateService().UpdateRoleAsync("Admin", "New", ["users.read"]);

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
        _rolesRepoMock.Setup(r => r.GetRoleByNameAsync("Unknown", It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var result = await CreateService().DeleteRoleAsync("Unknown");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsTrue_WhenRoleDeleted()
    {
        var id = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.GetRoleByNameAsync("Admin", It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = id, Name = "Admin" });
        _rolesRepoMock.Setup(r => r.DeleteRoleAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().DeleteRoleAsync("Admin");

        Assert.True(result);
    }

    // -------------------------------------------------------------------------
    // CreateGroupMappingAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateGroupMappingAsync_ReturnsNull_WhenMappingAlreadyExists()
    {
        var roleId = Guid.NewGuid();
        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingByGroupNameAsync("grp-admins", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupName = "grp-admins", RoleId = roleId });

        var result = await CreateService().CreateGroupMappingAsync("grp-admins", roleId);

        Assert.Null(result);
        _rolesRepoMock.Verify(r => r.CreateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupMappingAsync_CreatesAndReturnsMapping()
    {
        var roleId = Guid.NewGuid();
        var mapping = new GroupRoleMapping { GroupName = "grp-editors", RoleId = roleId };

        _rolesRepoMock.Setup(r => r.GetGroupRoleMappingByGroupNameAsync("grp-editors", It.IsAny<CancellationToken>())).ReturnsAsync((GroupRoleMapping?)null);
        _rolesRepoMock.Setup(r => r.CreateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>())).ReturnsAsync(mapping);

        var result = await CreateService().CreateGroupMappingAsync("grp-editors", roleId);

        Assert.NotNull(result);
        Assert.Equal("grp-editors", result!.GroupName);
    }

    // -------------------------------------------------------------------------
    // UpdateGroupMappingAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateGroupMappingAsync_ReturnsNull_WhenMappingNotFound()
    {
        _rolesRepoMock.Setup(r => r.GetAllGroupRoleMappingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateService().UpdateGroupMappingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateGroupMappingAsync_UpdatesAndReturnsMapping()
    {
        var id = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();
        var existing = new GroupRoleMapping { Id = id, GroupName = "grp-admins", RoleId = Guid.NewGuid() };
        var updated = new GroupRoleMapping { Id = id, GroupName = "grp-admins", RoleId = newRoleId };

        _rolesRepoMock.Setup(r => r.GetAllGroupRoleMappingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([existing]);
        _rolesRepoMock.Setup(r => r.UpdateGroupRoleMappingAsync(It.IsAny<GroupRoleMapping>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await CreateService().UpdateGroupMappingAsync(id, newRoleId);

        Assert.NotNull(result);
        Assert.Equal(newRoleId, result!.RoleId);
    }
}
