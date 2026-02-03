using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Authorization handler for context-aware policies
/// Evaluates complex authorization conditions including time, tenant, MFA, and custom claims
/// </summary>
public class ContextAwareAuthorizationHandler : AuthorizationHandler<ContextAwareRequirement>
{
    private readonly AuthorizationPoliciesOptions _options;
    private readonly IPermissionService _permissionService;
    private readonly ITenantContextService _tenantContextService;
    private readonly ILogger<ContextAwareAuthorizationHandler> _logger;

    public ContextAwareAuthorizationHandler(
        IOptions<AuthorizationPoliciesOptions> options,
        IPermissionService permissionService,
        ITenantContextService tenantContextService,
        ILogger<ContextAwareAuthorizationHandler> logger)
    {
        _options = options.Value;
        _permissionService = permissionService;
        _tenantContextService = tenantContextService;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ContextAwareRequirement requirement)
    {
        var policyName = requirement.PolicyName;

        if (!_options.ContextPolicies.TryGetValue(policyName, out var policyConfig))
        {
            _logger.LogWarning("Context policy {PolicyName} not found", policyName);
            return Task.CompletedTask;
        }

        _logger.LogDebug("Evaluating context-aware policy {PolicyName}", policyName);

        // Check all conditions
        if (!EvaluateRoleRequirements(context.User, policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: role requirement not met", policyName);
            return Task.CompletedTask;
        }

        if (!EvaluatePermissionRequirements(context.User, policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: permission requirement not met", policyName);
            return Task.CompletedTask;
        }

        if (!EvaluateTenantRequirement(policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: tenant requirement not met", policyName);
            return Task.CompletedTask;
        }

        if (!EvaluateTimeRestriction(policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: time restriction not met", policyName);
            return Task.CompletedTask;
        }

        if (!EvaluateMfaRequirement(context.User, policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: MFA requirement not met", policyName);
            return Task.CompletedTask;
        }

        if (!EvaluateCustomClaims(context.User, policyConfig))
        {
            _logger.LogDebug("Policy {PolicyName} failed: custom claim requirement not met", policyName);
            return Task.CompletedTask;
        }

        // All conditions passed
        _logger.LogInformation("Policy {PolicyName} succeeded for user", policyName);
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private bool EvaluateRoleRequirements(ClaimsPrincipal user, ContextAwarePolicyConfig config)
    {
        if (config.RequireRoles == null || config.RequireRoles.Count == 0)
        {
            return true; // No role requirement
        }

        // User must have at least one of the required roles
        foreach (var requiredRole in config.RequireRoles)
        {
            if (user.IsInRole(requiredRole))
            {
                return true;
            }
        }

        return false;
    }

    private bool EvaluatePermissionRequirements(ClaimsPrincipal user, ContextAwarePolicyConfig config)
    {
        if (config.RequirePermissions == null || config.RequirePermissions.Count == 0)
        {
            return true; // No permission requirement
        }

        // Get user's roles from claims
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Count == 0)
        {
            return false;
        }

        // Resolve permissions from roles
        var userPermissions = _permissionService.ResolvePermissions(roles);

        // User must have at least one of the required permissions
        foreach (var requiredPermission in config.RequirePermissions)
        {
            foreach (var userPermission in userPermissions)
            {
                if (_permissionService.MatchesPermission(requiredPermission, userPermission))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool EvaluateTenantRequirement(ContextAwarePolicyConfig config)
    {
        if (!config.RequireTenant)
        {
            return true; // No tenant requirement
        }

        var tenantContext = _tenantContextService.GetTenantContext();
        if (!tenantContext.IsValid)
        {
            return false;
        }

        // If specific tenants are allowed, check if current tenant is in the list
        if (config.AllowedTenants != null && config.AllowedTenants.Count > 0)
        {
            return config.AllowedTenants.Contains(tenantContext.TenantId);
        }

        return true;
    }

    private bool EvaluateTimeRestriction(ContextAwarePolicyConfig config)
    {
        if (config.TimeRestriction == null)
        {
            return true; // No time restriction
        }

        var timeRestriction = config.TimeRestriction;

        // Get current time in specified timezone
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeRestriction.Timezone);
        var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);

        // Check hour restriction
        var currentHour = currentTime.Hour;
        if (currentHour < timeRestriction.StartHour || currentHour >= timeRestriction.EndHour)
        {
            return false;
        }

        // Check day of week restriction
        if (timeRestriction.AllowedDays != null && timeRestriction.AllowedDays.Count > 0)
        {
            var currentDay = (int)currentTime.DayOfWeek;
            if (currentDay == 0) currentDay = 7; // Convert Sunday from 0 to 7

            if (!timeRestriction.AllowedDays.Contains(currentDay))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateMfaRequirement(ClaimsPrincipal user, ContextAwarePolicyConfig config)
    {
        if (!config.RequireMfa)
        {
            return true; // No MFA requirement
        }

        // Check for MFA claim from Azure AD
        // Azure AD sets "amr" (Authentication Methods Reference) claim
        var amrClaim = user.FindFirst("amr");
        if (amrClaim != null)
        {
            // MFA typically includes "mfa" in the amr claim
            return amrClaim.Value.Contains("mfa", StringComparison.OrdinalIgnoreCase);
        }

        // Alternative: check for acr (Authentication Context Class Reference)
        var acrClaim = user.FindFirst("acr");
        if (acrClaim != null)
        {
            // Multi-factor authentication context
            return acrClaim.Value.Contains("mfa", StringComparison.OrdinalIgnoreCase) ||
                   acrClaim.Value.Contains("multifactor", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private bool EvaluateCustomClaims(ClaimsPrincipal user, ContextAwarePolicyConfig config)
    {
        if (config.RequireCustomClaims == null || config.RequireCustomClaims.Count == 0)
        {
            return true; // No custom claim requirement
        }

        // All required custom claims must be present with correct values
        foreach (var (claimType, requiredValue) in config.RequireCustomClaims)
        {
            var claim = user.FindFirst(claimType);
            if (claim == null || !claim.Value.Equals(requiredValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
