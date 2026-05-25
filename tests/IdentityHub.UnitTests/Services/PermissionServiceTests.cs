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

public class PermissionServiceTests
{
    private readonly Mock<ILogger<PermissionService>> _loggerMock = new();
    private readonly Mock<IPermissionsRepository> _permissionsRepoMock = new();
    private readonly Mock<IRolesRepository> _rolesRepoMock = new();

    private PermissionService CreateService() =>
        new(_loggerMock.Object, _permissionsRepoMock.Object, _rolesRepoMock.Object);

    // -------------------------------------------------------------------------
    // ResolvePermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResolvePermissionsAsync_ReturnsEmpty_WhenRolesIsNull()
    {
        var result = await CreateService().ResolvePermissionsAsync(null!);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_ReturnsEmpty_WhenRolesIsEmpty()
    {
        var result = await CreateService().ResolvePermissionsAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_ReturnsPermissions_ForKnownRole()
    {
        var adminRole = new Role { Name = "Admin" };
        var adminRoleId = Guid.NewGuid();
        _permissionsRepoMock
            .Setup(r => r.GetAllRolePermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<string>>
            {
                [adminRoleId] = ["users.read", "users.write"]
            });

        adminRole.Id = adminRoleId;

        var result = await CreateService().ResolvePermissionsAsync(new List<Role> { adminRole });

        Assert.Equal(2, result.Count);
        Assert.Contains("users.read", result);
        Assert.Contains("users.write", result);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_DeduplicatesPermissions_AcrossRoles()
    {
        var adminRole = new Role { Name = "Admin" };
        var viewerRole = new Role { Name = "Viewer" };
        var adminRoleId = Guid.NewGuid();
        var viewerRoleId = Guid.NewGuid();
        _permissionsRepoMock
            .Setup(r => r.GetAllRolePermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<string>>
            {
                [adminRoleId] = ["users.read", "users.write"],
                [viewerRoleId] = ["users.read"]
            });

        adminRole.Id = adminRoleId;
        viewerRole.Id = viewerRoleId;

        var result = await CreateService().ResolvePermissionsAsync(new List<Role> { adminRole, viewerRole });

        // users.read appears in both roles — should only appear once
        Assert.Equal(2, result.Count);
        Assert.Single(result, p => p == "users.read");
    }

    [Fact]
    public async Task ResolvePermissionsAsync_ReturnsEmpty_WhenRoleNotInMapping()
    {
        var unknownRole = new Role { Name = "UnknownRole" };
        var adminRoleId = Guid.NewGuid();
        _permissionsRepoMock
            .Setup(r => r.GetAllRolePermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<string>>
            {
                [adminRoleId] = ["users.read"]
            });

        var result = await CreateService().ResolvePermissionsAsync(new List<Role> { unknownRole });

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_ReturnsEmpty_WhenRepositoryThrows()
    {
        var adminRole = new Role { Name = "Admin" };
        _permissionsRepoMock
            .Setup(r => r.GetAllRolePermissionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB unavailable"));

        var result = await CreateService().ResolvePermissionsAsync(new List<Role> { adminRole });

        // Should gracefully return empty rather than propagating the exception
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // MapGroupsToRolesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MapGroupsToRolesAsync_ReturnsEmpty_WhenGroupsIsNull()
    {
        var result = await CreateService().MapGroupsToRolesAsync(null!);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_ReturnsEmpty_WhenGroupsIsEmpty()
    {
        var result = await CreateService().MapGroupsToRolesAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_ReturnsMappedRoles()
    {
        var adminRole = new Role { Name = "Admin" };
        var viewerRole = new Role { Name = "Viewer" };
        var groupId = Guid.NewGuid();
        _rolesRepoMock
            .Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupId = groupId, RoleId = Guid.NewGuid(), Role = adminRole });

        var result = await CreateService().MapGroupsToRolesAsync([groupId.ToString()]);

        Assert.Single(result);
        Assert.Contains(result, r => r.Name == "Admin");
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_DeduplicatesRoles_WhenMultipleGroupsMapToSameRole()
    {
        var adminRole = new Role { Name = "Admin" };
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        _rolesRepoMock
            .Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupId = groupId1, RoleId = Guid.NewGuid(), Role = adminRole });
        _rolesRepoMock
            .Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupId = groupId2, RoleId = Guid.NewGuid(), Role = adminRole });

        var result = await CreateService().MapGroupsToRolesAsync([groupId1.ToString(), groupId2.ToString()]);

        Assert.Single(result);
        Assert.Equal("Admin", result[0].Name);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_SkipsUnknownGroups()
    {
        var adminRole = new Role { Name = "Admin" };
        var groupId = Guid.NewGuid();
        _rolesRepoMock
            .Setup(r => r.GetGroupRoleMappingByGroupIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupRoleMapping { GroupId = groupId, RoleId = Guid.NewGuid(), Role = adminRole });

        var result = await CreateService().MapGroupsToRolesAsync([Guid.NewGuid().ToString()]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_ReturnsEmpty_WhenRepositoryThrows()
    {
        _rolesRepoMock
            .Setup(r => r.GetGroupRoleMappingByGroupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB unavailable"));

        var result = await CreateService().MapGroupsToRolesAsync(["grp-admins"]);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // MatchesPermission
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("users.read", "users.read", true)]
    [InlineData("USERS.READ", "users.read", true)]       // case-insensitive exact match
    [InlineData("users.read", "users.*", true)]           // wildcard match
    [InlineData("users.write", "users.*", true)]          // wildcard match
    [InlineData("tickets.read", "users.*", false)]        // wrong prefix
    [InlineData("users", "users.*", false)]               // no dot separator
    [InlineData("users.read", "tickets.read", false)]     // no match
    [InlineData("", "users.*", false)]                    // empty permission
    [InlineData("users.read", "", false)]                 // empty pattern
    public void MatchesPermission_ReturnsExpected(string permission, string pattern, bool expected)
    {
        var result = CreateService().MatchesPermission(permission, pattern);
        Assert.Equal(expected, result);
    }

    // -------------------------------------------------------------------------
    // CreatePermissionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreatePermissionAsync_ReturnsNull_WhenPermissionAlreadyExists()
    {
        _permissionsRepoMock
            .Setup(r => r.GetPermissionByNameAsync("users.read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = Guid.NewGuid(), Name = "users.read" });

        var result = await CreateService().CreatePermissionAsync("users.read");

        Assert.Null(result);
        _permissionsRepoMock.Verify(r => r.CreatePermissionAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePermissionAsync_CreatesAndReturns_WhenNotExists()
    {
        var created = new Permission { Id = Guid.NewGuid(), Name = "users.delete" };
        _permissionsRepoMock
            .Setup(r => r.GetPermissionByNameAsync("users.delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);
        _permissionsRepoMock
            .Setup(r => r.CreatePermissionAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await CreateService().CreatePermissionAsync("users.delete");

        Assert.NotNull(result);
        Assert.Equal("users.delete", result.Name);
        _permissionsRepoMock.Verify(r => r.CreatePermissionAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeletePermissionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeletePermissionAsync_ReturnsFalse_WhenPermissionDoesNotExist()
    {
        _permissionsRepoMock
            .Setup(r => r.GetPermissionByNameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var result = await CreateService().DeletePermissionAsync("nonexistent");

        Assert.False(result);
        _permissionsRepoMock.Verify(r => r.DeletePermissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsTrue_WhenPermissionDeleted()
    {
        var id = Guid.NewGuid();
        _permissionsRepoMock
            .Setup(r => r.GetPermissionByNameAsync("users.read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = id, Name = "users.read" });
        _permissionsRepoMock
            .Setup(r => r.DeletePermissionAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().DeletePermissionAsync("users.read");

        Assert.True(result);
    }

    // -------------------------------------------------------------------------
    // GetAllPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsList()
    {
        var perms = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), Name = "users.read" },
            new() { Id = Guid.NewGuid(), Name = "users.write" }
        };
        _permissionsRepoMock
            .Setup(r => r.GetAllPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(perms);

        var result = await CreateService().GetAllPermissionsAsync();

        Assert.Equal(2, result.Count);
    }
}
