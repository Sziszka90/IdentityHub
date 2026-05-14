using IdentityHub.Application.Interfaces;

namespace IdentityHub.API.Middleware;

/// <summary>
/// Middleware to validate tenant context for each request.
/// Rejects requests with invalid or missing tenant context.
/// </summary>
public class TenantContextValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextValidationMiddleware> _logger;

    public TenantContextValidationMiddleware(RequestDelegate next, ILogger<TenantContextValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextService tenantContextService)
    {
        var tenantContext = tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            _logger.LogWarning("Invalid or missing tenant context for request {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing tenant context." });
            return;
        }

        await _next(context);
    }
}
