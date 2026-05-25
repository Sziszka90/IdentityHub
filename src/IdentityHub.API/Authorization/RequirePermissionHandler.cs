using System.Security.Claims;
using IdentityHub.API.Constants;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
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
            var userId = context.User.FindFirstValue(ClaimConstants.OBJECT_ID);
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            var userPermissions = await _userService.GetUserPermissionsAsync(userId);

            if (_userService is UserService concreteUserService &&
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
