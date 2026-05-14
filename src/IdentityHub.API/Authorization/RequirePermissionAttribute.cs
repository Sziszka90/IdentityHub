using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.API.Authorization
{
    /// <summary>
    /// Attribute to require that the current user has a specific permission.
    /// Usage: [RequirePermission("users.read")]
    /// </summary>
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permission) : base(policy: $"RequirePermission:{permission}")
        {
        }
    }
}
