using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Handler for role-based authorization
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IUserContextService _userContextService;

    public RoleAuthorizationHandler(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var userContext = _userContextService.GetUserContext(context.User);

        if (requirement.Roles.Any(role => userContext.Roles.Contains(role)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
