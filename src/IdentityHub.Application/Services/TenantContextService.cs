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
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantContext GetTenantContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new TenantContext();
        }

        // Get from Items (set by middleware)
        if (httpContext.Items.TryGetValue("TenantContext", out var contextObj)
            && contextObj is TenantContext tenantContext)
        {
            return tenantContext;
        }

        // If middleware didn't set it, tenant validation failed
        return new TenantContext();
    }

    public bool ValidateTenantContext(TenantContext context)
    {
        return context?.IsValid ?? false;
    }

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
