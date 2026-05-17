using IdentityHub.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace IdentityHub.IntegrationTests.Infrastructure.Fakes;

/// <summary>
/// Bypasses RequirePermission policy checks in integration tests.
/// </summary>
public class PermissiveAuthorizationHandler : AuthorizationHandler<RequirePermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequirePermissionRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Bypasses the [Authorize] default-policy check (DenyAnonymousAuthorizationRequirement)
/// so that controllers marked with [Authorize] are not blocked by the missing JWT Bearer token.
/// </summary>
public class PermissiveDenyAnonymousHandler : AuthorizationHandler<DenyAnonymousAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DenyAnonymousAuthorizationRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
