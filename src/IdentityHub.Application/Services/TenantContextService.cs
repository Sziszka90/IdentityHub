using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing tenant context
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantConfigurationOptions _tenantOptions;

    public TenantContextService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TenantConfigurationOptions> tenantOptions)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _tenantOptions = tenantOptions?.Value ?? throw new ArgumentNullException(nameof(tenantOptions));
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

        // Try to get user id from claims
        string? userId = httpContext.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        if (httpContext.Request.Headers.TryGetValue(_tenantOptions.HeaderName, out var tenantId))
        {
            httpContext.Items["TenantContext"] = new TenantContext()
            {
                UserId = userId ?? string.Empty,
                TenantId = tenantId.ToString()
            };
        }
        else
        {
            if (userId is not null)
            {
                var userTenantMappingsRepository = httpContext.RequestServices.GetService<IUserTenantMappingsRepository>();
                var mapping = userTenantMappingsRepository?.GetUserTenantMappingByUserId(userId);
                if (mapping is not null)
                {
                    var mappedContext = new TenantContext
                    {
                        UserId = mapping.UserId,
                        TenantId = mapping.TenantId
                    };

                    httpContext.Items["TenantContext"] = mappedContext;
                    return mappedContext;
                }
            }
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
