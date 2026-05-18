using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Http;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing tenant context
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current tenant context from the active HTTP request.
    /// </summary>
    /// <returns>The <see cref="TenantContext"/> for the current request, or an empty context if unavailable.</returns>
    public TenantContext GetTenantContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new TenantContext();
        }


        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            // Try to get user id from claims
            string? userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            httpContext.Items["TenantContext"] = new TenantContext()
            {
                UserId = userId ?? string.Empty,
                TenantId = tenantId.ToString()
            };
        }

        if (httpContext.Items.TryGetValue("TenantContext", out var contextObj)
            && contextObj is TenantContext tenantContext)
        {
            return tenantContext;
        }

        return new TenantContext();
    }

    /// <summary>
    /// Checks whether the specified user belongs to the specified tenant.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="tenantId">The tenant's unique identifier.</param>
    /// <returns><c>true</c> if the current context matches the given user and tenant; otherwise <c>false</c>.</returns>
    public bool UserBelongsToTenant(string userId, string tenantId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return false;
        }

        var currentContext = GetTenantContext();
        return currentContext.TenantId == tenantId && currentContext.UserId == userId;
    }
}
