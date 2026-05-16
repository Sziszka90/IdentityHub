
using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Client.Authorization;

/// <summary>
/// Marks a controller or action as requiring the caller to hold the specified
/// IdentityHub permission. Works in both the IdentityHub API itself and in any
/// external consumer app that has installed the IdentityHub.Client NuGet.
///
/// The attribute only sets an ASP.NET Core policy name — the actual permission
/// check is handled by whichever handler is registered in the DI container:
///
/// • Inside IdentityHub.API:
///     DynamicPermissionPolicyProvider + RequirePermissionHandler are registered
///     automatically. The handler calls IUserService directly (in-process, no HTTP).
///
/// • In a consumer app (NuGet scenario):
///     Call AddIdentityHubAuthorization() at startup. The handler calls
///     IIdentityHubClient.CheckPermissionAsync() — one HTTP call to IdentityHub.API.
///
/// Usage:
///   [RequirePermission("users.read")]
///   public IActionResult GetUsers() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(policy: $"{PermissionPolicyProvider.PolicyPrefix}{permission}")
    {
    }
}
