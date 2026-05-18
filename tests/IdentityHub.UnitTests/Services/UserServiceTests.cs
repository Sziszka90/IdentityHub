using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<ITenantContextService> _tenantContextMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IGraphService> _graphServiceMock = new();
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();

    private UserService CreateService() =>
        new(_tenantContextMock.Object, _permissionServiceMock.Object, _graphServiceMock.Object, _roleServiceMock.Object, _loggerMock.Object);

    private void SetupTenantContext(string tenantId = "tenant-123", string userId = "")
    {
        _tenantContextMock.Setup(t => t.GetTenantContext())
            .Returns(new TenantContext { TenantId = tenantId, UserId = userId });
    }

    // -------------------------------------------------------------------------
    // GetUserPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserPermissionsAsync_ThrowsArgumentException_WhenUserIdIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().GetUserPermissionsAsync(""));
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ThrowsKeyNotFoundException_WhenUserNotFound()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().GetUserPermissionsAsync("u1"));
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsDto_WithResolvedPermissions()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync(new User { Id = "u1", Mail = "u@contoso.com", DisplayName = "Alice" });
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync(["grp-admins"]);
        var adminRole = new Role { Name = "Admin" };
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { adminRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string> { "users.read" });

        var result = await CreateService().GetUserPermissionsAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result.UserId);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("users.read", result.Permissions);
    }

    // -------------------------------------------------------------------------
    // GetUsersWithPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUsersWithPermissionsAsync_ReturnsEmpty_WhenNoUsers()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);

        var result = await CreateService().GetUsersWithPermissionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersWithPermissionsAsync_SkipsUsersWithNoId()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([new User { Id = null, DisplayName = "Ghost" }]);

        var result = await CreateService().GetUsersWithPermissionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersWithPermissionsAsync_ReturnsPermissionsForAllUsers()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([new User { Id = "u1", Mail = "a@b.com", DisplayName = "Alice" }]);
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync(["grp-admins"]);
        var adminRole = new Role { Name = "Admin" };
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { adminRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string> { "users.read" });

        var result = await CreateService().GetUsersWithPermissionsAsync();

        Assert.Single(result);
        Assert.Equal("u1", result[0].UserId);
        Assert.Contains("users.read", result[0].Permissions);
    }

    // -------------------------------------------------------------------------
    // UserHasPermissionAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("", "users.read")]
    [InlineData("u1", "")]
    [InlineData(null, "users.read")]
    [InlineData("u1", null)]
    public async Task UserHasPermissionAsync_ReturnsFalse_WhenInputIsNullOrEmpty(string? userId, string? permission)
    {
        var result = await CreateService().UserHasPermissionAsync(userId!, permission!);
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasPermissionAsync_ReturnsTrue_WhenUserHasPermission()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync(new User { Id = "u1" });
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync([]);
        var adminRole = new Role { Name = "Admin" };
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { adminRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string> { "users.read", "users.write" });

        var result = await CreateService().UserHasPermissionAsync("u1", "users.read");

        Assert.True(result);
    }

    [Fact]
    public async Task UserHasPermissionAsync_ReturnsFalse_WhenUserDoesNotHavePermission()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync(new User { Id = "u1" });
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync([]);
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role>());
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        var result = await CreateService().UserHasPermissionAsync("u1", "admin.delete");

        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // AssignRolesToUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsNull_WhenUserIdIsEmpty()
    {
        var result = await CreateService().AssignRolesToUserAsync("", [Guid.NewGuid().ToString()]);
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsNull_WhenRoleIdsIsEmpty()
    {
        var result = await CreateService().AssignRolesToUserAsync("u1", []);
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsNull_WhenUserNotFound()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync((User?)null);

        var result = await CreateService().AssignRolesToUserAsync("u1", [Guid.NewGuid().ToString()]);

        Assert.Null(result);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsDto_WhenSuccessful()
    {
        SetupTenantContext();
        var roleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var mapping = new GroupRoleMapping { GroupId = groupId, RoleId = roleId };

        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync(new User { Id = "u1", Mail = "u@c.com" });
        _roleServiceMock.Setup(r => r.GetGroupMappingByRoleIdAsync(roleId, default)).ReturnsAsync(mapping);
        _graphServiceMock.Setup(g => g.AddUserToGroupsAsync("u1", It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        var adminRole2 = new Role { Name = "Admin" };
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { adminRole2 });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string> { "users.read" });

        var result = await CreateService().AssignRolesToUserAsync("u1", [roleId.ToString()]);

        Assert.NotNull(result);
        Assert.Equal("u1", result!.UserId);
        Assert.Contains(groupId.ToString(), result.Groups);
    }

    // -------------------------------------------------------------------------
    // RemoveRolesFromUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveRolesFromUserAsync_ReturnsNull_WhenUserIdIsEmpty()
    {
        var result = await CreateService().RemoveRolesFromUserAsync("", [Guid.NewGuid().ToString()]);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_ReturnsNull_WhenUserNotFound()
    {
        SetupTenantContext();
        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync((User?)null);

        var result = await CreateService().RemoveRolesFromUserAsync("u1", [Guid.NewGuid().ToString()]);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_ReturnsRemainingPermissions_WhenSuccessful()
    {
        SetupTenantContext();
        var roleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var mapping = new GroupRoleMapping { GroupId = groupId, RoleId = roleId };

        _graphServiceMock.Setup(g => g.GetUserAsync("u1")).ReturnsAsync(new User { Id = "u1", Mail = "u@c.com" });
        _roleServiceMock.Setup(r => r.GetGroupMappingByRoleIdAsync(roleId, default)).ReturnsAsync(mapping);
        _graphServiceMock.Setup(g => g.RemoveUserFromGroupsAsync("u1", It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("u1")).ReturnsAsync([]);
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role>());
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        var result = await CreateService().RemoveRolesFromUserAsync("u1", [roleId.ToString()]);

        Assert.NotNull(result);
        Assert.Equal("u1", result!.UserId);
        Assert.Empty(result.Permissions);
    }

    // -------------------------------------------------------------------------
    // CreateUserWithRolesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUserWithRolesAsync_ReturnsNull_WhenGraphCreationFails()
    {
        _graphServiceMock.Setup(g => g.CreateUserAsync(It.IsAny<User>()))!.ReturnsAsync((User?)null);

        var result = await CreateService().CreateUserWithRolesAsync(new User { UserPrincipalName = "test@c.com" }, []);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserWithRolesAsync_ReturnsCreatedUser_WithNoRoles()
    {
        var user = new User { Id = "u-new", UserPrincipalName = "new@c.com" };
        _graphServiceMock.Setup(g => g.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await CreateService().CreateUserWithRolesAsync(user, []);

        Assert.NotNull(result);
        Assert.Equal("u-new", result!.Id);
    }

    [Fact]
    public async Task CreateUserWithRolesAsync_AssignsGroupsForValidRoleIds()
    {
        var roleId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var user = new User { Id = "u-new", UserPrincipalName = "new@c.com" };
        var mapping = new GroupRoleMapping { GroupId = groupId, RoleId = roleId };

        _graphServiceMock.Setup(g => g.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(user);
        _roleServiceMock.Setup(r => r.GetGroupMappingByRoleIdAsync(roleId, default)).ReturnsAsync(mapping);
        _graphServiceMock.Setup(g => g.AddUserToGroupsAsync("u-new", It.IsAny<List<string>>())).Returns(Task.CompletedTask);

        var result = await CreateService().CreateUserWithRolesAsync(user, [roleId.ToString()]);

        Assert.NotNull(result);
        _graphServiceMock.Verify(g => g.AddUserToGroupsAsync("u-new", It.Is<List<string>>(l => l.Contains(groupId.ToString()))), Times.Once);
    }
}
