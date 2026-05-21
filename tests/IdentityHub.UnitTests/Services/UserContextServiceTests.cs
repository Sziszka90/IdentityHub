using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

public class UserContextServiceTests
{
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IGraphService> _graphServiceMock = new();
    private readonly Mock<ILogger<UserContextService>> _loggerMock = new();
    private readonly Mock<ITenantContextService> _tenantContextServiceMock = new();

    private UserContextService CreateService() => new(_permissionServiceMock.Object, _graphServiceMock.Object, _loggerMock.Object, _tenantContextServiceMock.Object);

    private static ClaimsPrincipal AuthenticatedPrincipal(params (string type, string value)[] claims)
    {
        var claimList = new List<Claim>(claims.Select(c => new Claim(c.type, c.value)));
        var identity = new ClaimsIdentity(claimList, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    // -------------------------------------------------------------------------
    // GetUserContext
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserContext_ReturnsUnauthenticated_WhenPrincipalIsNull()
    {
        var result = await CreateService().GetUserContext(null!);
        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task GetUserContext_ReturnsUnauthenticated_WhenPrincipalNotAuthenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // no auth type → not authenticated
        var result = await CreateService().GetUserContext(principal);
        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task GetUserContext_ReturnsUnauthenticated_WhenTenantIdMissing()
    {
        var principal = AuthenticatedPrincipal(("oid", "user-123")); // no "tid" claim
        var result = await CreateService().GetUserContext(principal);
        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task GetUserContext_ReturnsAuthenticatedContext_WithCorrectClaims()
    {
        _tenantContextServiceMock.Setup(t => t.GetTenantContext()).Returns(new TenantContext { TenantId = "tenant-abc" });
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role>());
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        var principal = AuthenticatedPrincipal(
            ("tid", "tenant-abc"),
            ("oid", "user-123"),
            ("preferred_username", "alice@contoso.com"),
            ("name", "Alice"));

        var result = await CreateService().GetUserContext(principal);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("tenant-abc", result.TenantId);
        Assert.Equal("alice@contoso.com", result.Email);
        Assert.Equal("Alice", result.DisplayName);
    }

    [Fact]
    public async Task GetUserContext_FallsBackToNameIdentifier_WhenOidMissing()
    {
        _tenantContextServiceMock.Setup(t => t.GetTenantContext()).Returns(new TenantContext { TenantId = "tenant-abc" });
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role>());
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        var principal = AuthenticatedPrincipal(
            ("tid", "tenant-abc"),
            (ClaimTypes.NameIdentifier, "fallback-id"));

        var result = await CreateService().GetUserContext(principal);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("fallback-id", result.UserId);
    }

    [Fact]
    public async Task GetUserContext_ResolvesGroupsAndRolesAndPermissions()
    {
        _tenantContextServiceMock.Setup(t => t.GetTenantContext()).Returns(new TenantContext { TenantId = "tenant-abc" });
        var adminRole = new Role { Name = "Admin" };
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync("user-123")).ReturnsAsync(new List<string> { "grp-admins" });
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.Is<List<string>>(l => l.Contains("grp-admins"))))
            .ReturnsAsync(new List<Role> { adminRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>()))
            .ReturnsAsync(new List<string> { "users.read", "users.write" });

        var principal = AuthenticatedPrincipal(
            ("tid", "tenant-abc"),
            ("oid", "user-123"));

        var result = await CreateService().GetUserContext(principal);

        Assert.Contains("Admin", result.Roles);
        Assert.Contains("users.read", result.Permissions);
        Assert.Contains("users.write", result.Permissions);
    }

    [Fact]
    public async Task GetUserContext_MergesTokenRolesAndGroupRoles()
    {
        _tenantContextServiceMock.Setup(t => t.GetTenantContext()).Returns(new TenantContext { TenantId = "tenant-abc" });
        var groupRole = new Role { Name = "GroupRole" };
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { groupRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        var principal = AuthenticatedPrincipal(
            ("tid", "tenant-abc"),
            ("oid", "user-123"),
            ("roles", "TokenRole"));

        var result = await CreateService().GetUserContext(principal);

        Assert.Contains("TokenRole", result.Roles);
        Assert.Contains("GroupRole", result.Roles);
    }

    [Fact]
    public async Task GetUserContext_DeduplicatesRoles()
    {
        _tenantContextServiceMock.Setup(t => t.GetTenantContext()).Returns(new TenantContext { TenantId = "tenant-abc" });
        var adminRole = new Role { Name = "Admin" };
        _graphServiceMock.Setup(g => g.GetUserTransitiveGroupIdsAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        _permissionServiceMock.Setup(p => p.MapGroupsToRolesAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Role> { adminRole });
        _permissionServiceMock.Setup(p => p.ResolvePermissionsAsync(It.IsAny<IEnumerable<Role>>())).ReturnsAsync(new List<string>());

        // "Admin" appears in both token roles and group-derived roles
        var principal = AuthenticatedPrincipal(
            ("tid", "tenant-abc"),
            ("oid", "user-123"),
            ("roles", "Admin"));

        var result = await CreateService().GetUserContext(principal);

        Assert.Single(result.Roles, r => r == "Admin");
    }

    // -------------------------------------------------------------------------
    // ValidateUserContext
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateUserContext_ReturnsTrue_WhenContextIsValid()
    {
        var ctx = new UserContext { IsAuthenticated = true, UserId = "u1", TenantId = "t1" };
        Assert.True(CreateService().ValidateUserContext(ctx));
    }

    [Fact]
    public void ValidateUserContext_ReturnsFalse_WhenNotAuthenticated()
    {
        var ctx = new UserContext { IsAuthenticated = false, UserId = "u1", TenantId = "t1" };
        Assert.False(CreateService().ValidateUserContext(ctx));
    }

    [Fact]
    public void ValidateUserContext_ReturnsFalse_WhenUserIdEmpty()
    {
        var ctx = new UserContext { IsAuthenticated = true, UserId = "", TenantId = "t1" };
        Assert.False(CreateService().ValidateUserContext(ctx));
    }

    [Fact]
    public void ValidateUserContext_ReturnsFalse_WhenTenantIdEmpty()
    {
        var ctx = new UserContext { IsAuthenticated = true, UserId = "u1", TenantId = "" };
        Assert.False(CreateService().ValidateUserContext(ctx));
    }
}
