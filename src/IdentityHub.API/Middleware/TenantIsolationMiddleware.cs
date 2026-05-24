using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.Options;

namespace IdentityHub.API.Middleware;

/// <summary>
/// Middleware to extract and validate tenant context from JWT claims
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;
    private readonly TenantConfigurationOptions _tenantOptions;

    public TenantIsolationMiddleware(
        RequestDelegate next,
        ILogger<TenantIsolationMiddleware> logger,
        IOptions<TenantConfigurationOptions> tenantOptions)
    {
        _next = next;
        _logger = logger;
        _tenantOptions = tenantOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, IUserTenantMappingsRepository userTenantMappingsRepository)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path == "/api/identity/me" || path == "/api/identity/status")
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated is not true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Missing user ID in JWT token");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant context" });
            return;
        }

        var mapping = userTenantMappingsRepository.GetUserTenantMappingByUserId(userId);
        if (mapping is null || string.IsNullOrWhiteSpace(mapping.TenantId))
        {
            _logger.LogWarning("No tenant mapping found for user {UserId}", userId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not allowed" });
            return;
        }

        var tenantId = mapping.TenantId;

        if (_tenantOptions.AllowedTenantIds.Count > 0
            && !_tenantOptions.AllowedTenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejected request for disallowed tenant {TenantId}", tenantId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not allowed" });
            return;
        }

        var tenantContext = new TenantContext
        {
            TenantId = tenantId,
            UserId = userId ?? string.Empty
        };

        context.Items["TenantContext"] = tenantContext;

        _logger.LogDebug("Tenant context established: TenantId={TenantId}, UserId={UserId}",
            tenantId, userId);

        await _next(context);
    }
}

/// <summary>
/// Extension methods for tenant isolation middleware
/// </summary>
public static class TenantIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIsolation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantIsolationMiddleware>();
    }
}
