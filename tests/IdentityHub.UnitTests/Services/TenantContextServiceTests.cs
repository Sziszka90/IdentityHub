using IdentityHub.Application.Services;
using Microsoft.AspNetCore.Http;
using IdentityHub.Domain.Models;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

public class TenantContextServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    private TenantContextService CreateService() => new(_httpContextAccessorMock.Object);

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
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

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
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var result = CreateService().GetTenantContext();

        Assert.NotNull(result);
        Assert.Empty(result.TenantId);
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
