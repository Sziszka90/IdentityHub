using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IdentityHub.Application.Interfaces;

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
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            var userPermissions = await _userService.GetUserPermissionsAsync(userId);
            if (userPermissions != null && userPermissions.Permissions.Contains(requirement.Permission, System.StringComparer.OrdinalIgnoreCase))
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
