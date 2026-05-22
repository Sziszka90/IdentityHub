using System.Security.Claims;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.API.Authorization
{
    public class RequirePermissionHandler : AuthorizationHandler<RequirePermissionRequirement>
    {
        private readonly IUserService _userService;

        public RequirePermissionHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequirePermissionRequirement requirement)
        {
            var userId = context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            var userPermissions = await _userService.GetUserPermissionsAsync(userId);
            if (_userService is IdentityHub.Application.Services.UserService concreteUserService &&
                concreteUserService.HasPermission(userPermissions, requirement.Permission))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    public class RequirePermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public RequirePermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
