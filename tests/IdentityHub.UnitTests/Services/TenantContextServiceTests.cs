using System;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

public class TenantContextServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IUserTenantMappingsRepository> _userTenantMappingsRepositoryMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly IOptions<TenantConfigurationOptions> _tenantOptions = Options.Create(new TenantConfigurationOptions { HeaderName = "X-Tenant-Id" });

    private TenantContextService CreateService() => new(_httpContextAccessorMock.Object, _tenantOptions, NullLogger<TenantContextService>.Instance);

    // -------------------------------------------------------------------------
    // GetTenantContext
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTenantContext_ReturnsEmptyContext_WhenHttpContextIsNull()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null!);

        var result = CreateService().GetTenantContext();

        Assert.NotNull(result);
        Assert.Empty(result.TenantId);
        Assert.Empty(result.UserId);
    }

    [Fact]
    public void GetTenantContext_ReturnsEmptyContext_WhenTenantContextNotInItems()
    {
        var httpContext = new DefaultHttpContext();
        // Items is empty — no TenantContext stored
        httpContext.RequestServices = _serviceProviderMock.Object;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _serviceProviderMock
            .Setup(s => s.GetService(typeof(IUserTenantMappingsRepository)))
            .Returns(_userTenantMappingsRepositoryMock.Object);
        _userTenantMappingsRepositoryMock.Setup(r => r.GetUserTenantMappingByUserId(It.IsAny<string>())).Returns((UserTenantMapping?)null);

        var result = CreateService().GetTenantContext();

        Assert.NotNull(result);
        Assert.Empty(result.TenantId);
    }

    [Fact]
    public void GetTenantContext_ReturnsTenantContext_WhenPresentInItems()
    {
        var expected = new TenantContext { TenantId = "tenant-123", UserId = "user-456" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantContext"] = expected;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var result = CreateService().GetTenantContext();

        Assert.Equal("tenant-123", result.TenantId);
        Assert.Equal("user-456", result.UserId);
    }

    [Fact]
    public void GetTenantContext_ReturnsEmptyContext_WhenItemValueIsWrongType()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantContext"] = "not a TenantContext";
        httpContext.RequestServices = _serviceProviderMock.Object;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _serviceProviderMock
            .Setup(s => s.GetService(typeof(IUserTenantMappingsRepository)))
            .Returns(_userTenantMappingsRepositoryMock.Object);
        _userTenantMappingsRepositoryMock.Setup(r => r.GetUserTenantMappingByUserId(It.IsAny<string>())).Returns((UserTenantMapping?)null);

        var result = CreateService().GetTenantContext();

        Assert.NotNull(result);
        Assert.Empty(result.TenantId);
    }

    [Fact]
    public void GetTenantContext_ReturnsMappedTenant_WhenHeaderMissingAndUserExistsInDb()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = _serviceProviderMock.Object;
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-456")
            ],
            "TestAuth"));
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _serviceProviderMock
            .Setup(s => s.GetService(typeof(IUserTenantMappingsRepository)))
            .Returns(_userTenantMappingsRepositoryMock.Object);
        _userTenantMappingsRepositoryMock.Setup(r => r.GetUserTenantMappingByUserId("user-456"))
            .Returns(new UserTenantMapping { UserId = "user-456", TenantId = "tenant-123" });

        var result = CreateService().GetTenantContext();

        Assert.Equal("tenant-123", result.TenantId);
        Assert.Equal("user-456", result.UserId);
    }

    [Fact]
    public void GetTenantContext_ReturnsHeaderTenant_WhenHeaderMatchesDbMapping()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = _serviceProviderMock.Object;
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-123";
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-456")
            ],
            "TestAuth"));
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _serviceProviderMock
            .Setup(s => s.GetService(typeof(IUserTenantMappingsRepository)))
            .Returns(_userTenantMappingsRepositoryMock.Object);
        _userTenantMappingsRepositoryMock.Setup(r => r.GetUserTenantMappingByUserId("user-456"))
            .Returns(new UserTenantMapping { UserId = "user-456", TenantId = "tenant-123" });

        var result = CreateService().GetTenantContext();

        Assert.Equal("tenant-123", result.TenantId);
        Assert.Equal("user-456", result.UserId);
    }

    [Fact]
    public void GetTenantContext_ReturnsEmptyContext_WhenHeaderDoesNotMatchDbMapping()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = _serviceProviderMock.Object;
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-header";
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-456")
            ],
            "TestAuth"));
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _serviceProviderMock
            .Setup(s => s.GetService(typeof(IUserTenantMappingsRepository)))
            .Returns(_userTenantMappingsRepositoryMock.Object);
        _userTenantMappingsRepositoryMock.Setup(r => r.GetUserTenantMappingByUserId("user-456"))
            .Returns(new UserTenantMapping { UserId = "user-456", TenantId = "tenant-123" });

        var result = CreateService().GetTenantContext();

        Assert.NotNull(result);
        Assert.Empty(result.TenantId);
        Assert.Empty(result.UserId);
    }

    // -------------------------------------------------------------------------
    // UserBelongsToTenant
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("", "tenant-123")]
    [InlineData("user-456", "")]
    [InlineData("", "")]
    public void UserBelongsToTenant_ReturnsFalse_WhenInputIsEmpty(string userId, string tenantId)
    {
        var result = CreateService().UserBelongsToTenant(userId, tenantId);
        Assert.False(result);
    }

    [Fact]
    public void UserBelongsToTenant_ReturnsTrue_WhenContextMatches()
    {
        var tenantContext = new TenantContext { TenantId = "tenant-123", UserId = "user-456" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantContext"] = tenantContext;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var result = CreateService().UserBelongsToTenant("user-456", "tenant-123");

        Assert.True(result);
    }

    [Fact]
    public void UserBelongsToTenant_ReturnsFalse_WhenUserIdDoesNotMatch()
    {
        var tenantContext = new TenantContext { TenantId = "tenant-123", UserId = "user-456" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantContext"] = tenantContext;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var result = CreateService().UserBelongsToTenant("other-user", "tenant-123");

        Assert.False(result);
    }

    [Fact]
    public void UserBelongsToTenant_ReturnsFalse_WhenTenantIdDoesNotMatch()
    {
        var tenantContext = new TenantContext { TenantId = "tenant-123", UserId = "user-456" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantContext"] = tenantContext;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var result = CreateService().UserBelongsToTenant("user-456", "other-tenant");

        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // TenantContext.IsValid
    // -------------------------------------------------------------------------

    [Fact]
    public void TenantContextIsValid_ReturnsFalse_WhenTenantIdEmpty()
    {
        var ctx = new TenantContext { TenantId = "" };
        Assert.False(ctx.IsValid);
    }

    [Fact]
    public void TenantContextIsValid_ReturnsTrue_WhenTenantIdSet()
    {
        var ctx = new TenantContext { TenantId = "tenant-abc" };
        Assert.True(ctx.IsValid);
    }
}
