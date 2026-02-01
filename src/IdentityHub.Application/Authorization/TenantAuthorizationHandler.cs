using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Handler for tenant-based authorization
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    private readonly ITenantContextService _tenantContextService;
    private readonly ILogger<TenantAuthorizationHandler> _logger;

    public TenantAuthorizationHandler(
        ITenantContextService tenantContextService,
        ILogger<TenantAuthorizationHandler> logger)
    {
        _tenantContextService = tenantContextService;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Tenant authorization failed: User not authenticated");
            return Task.CompletedTask;
        }

        var tenantContext = _tenantContextService.GetTenantContext();

        if (!_tenantContextService.ValidateTenantContext(tenantContext))
        {
            _logger.LogWarning("Tenant authorization failed: Invalid tenant context for user {UserId}",
                tenantContext.UserId);
            return Task.CompletedTask;
        }

        _logger.LogDebug("Tenant authorization succeeded for user {UserId} in tenant {TenantId}",
            tenantContext.UserId, tenantContext.TenantId);

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
