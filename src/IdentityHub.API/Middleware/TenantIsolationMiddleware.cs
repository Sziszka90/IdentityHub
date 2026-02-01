using IdentityHub.Domain.Models;

namespace IdentityHub.API.Middleware;

/// <summary>
/// Middleware to extract and validate tenant context from JWT claims
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for unauthenticated requests
        if (context.User?.Identity?.IsAuthenticated is not true)
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from claims
        var tenantId = context.User.FindFirst("tid")?.Value
                    ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst("oid")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Missing tenant ID in JWT token for user {UserId}", userId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant context" });
            return;
        }

        // Store tenant context in request items
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
