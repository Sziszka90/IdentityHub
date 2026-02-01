using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Handler for permission-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserContextService _userContextService;

    public PermissionAuthorizationHandler(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var userContext = _userContextService.GetUserContext(context.User);

        if (userContext.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
