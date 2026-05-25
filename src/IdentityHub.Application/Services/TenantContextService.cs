using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Services;

/// <summary>
/// Service for managing tenant context
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantConfigurationOptions _tenantOptions;
    private readonly ILogger<TenantContextService> _logger;

    public TenantContextService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TenantConfigurationOptions> tenantOptions,
        ILogger<TenantContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _tenantOptions = tenantOptions?.Value ?? throw new ArgumentNullException(nameof(tenantOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogDebug("No active HttpContext was available while resolving tenant context");
            return new TenantContext();
        }

        string? userId = httpContext.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        string? tenantIdFromHeader = null;

        if (httpContext.Request.Headers.TryGetValue(_tenantOptions.HeaderName, out var tenantIdHeader))
        {
            tenantIdFromHeader = tenantIdHeader.ToString();
            _logger.LogDebug("Tenant header {HeaderName} was present for user {UserId}", _tenantOptions.HeaderName, userId ?? "<anonymous>");
        }

        UserTenantMapping? mapping = null;

        if (userId is not null)
        {
            var userTenantMappingsRepository = httpContext.RequestServices.GetService<IUserTenantMappingsRepository>();
            mapping = userTenantMappingsRepository?.GetUserTenantMappingByUserId(userId);
        }

        if (mapping is not null && !string.IsNullOrWhiteSpace(tenantIdFromHeader) &&
            !string.Equals(tenantIdFromHeader, mapping.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Tenant header {HeaderTenantId} did not match mapped tenant {MappedTenantId} for user {UserId}",
                tenantIdFromHeader,
                mapping.TenantId,
                userId);
            return new TenantContext();
        }

        if (!string.IsNullOrWhiteSpace(tenantIdFromHeader))
        {
            httpContext.Items["TenantContext"] = new TenantContext()
            {
                UserId = userId ?? string.Empty,
                TenantId = tenantIdFromHeader
            };

            _logger.LogDebug(
                "Resolved tenant context from request header for user {UserId} and tenant {TenantId}",
                userId ?? string.Empty,
                tenantIdFromHeader);
        }
        else if (mapping is not null)
        {
            var mappedContext = new TenantContext
            {
                UserId = mapping.UserId,
                TenantId = mapping.TenantId
            };

            httpContext.Items["TenantContext"] = mappedContext;
            _logger.LogDebug(
                "Resolved tenant context from user mapping for user {UserId} and tenant {TenantId}",
                mapping.UserId,
                mapping.TenantId);
            return mappedContext;
        }

        if (httpContext.Items.TryGetValue("TenantContext", out var contextObj)
            && contextObj is TenantContext tenantContext)
        {
            _logger.LogDebug(
                "Resolved tenant context from HttpContext items for user {UserId} and tenant {TenantId}",
                tenantContext.UserId,
                tenantContext.TenantId);
            return tenantContext;
        }

        _logger.LogDebug("No tenant context could be resolved for user {UserId}", userId ?? "<anonymous>");
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
            _logger.LogDebug("UserBelongsToTenant called with incomplete values. UserId: {UserId}, TenantId: {TenantId}", userId, tenantId);
            return false;
        }

        var currentContext = GetTenantContext();
        var belongsToTenant = currentContext.TenantId == tenantId && currentContext.UserId == userId;
        _logger.LogDebug(
            "Evaluated tenant membership for user {UserId} against tenant {TenantId}: {BelongsToTenant}",
            userId,
            tenantId,
            belongsToTenant);
        return belongsToTenant;
    }
}
