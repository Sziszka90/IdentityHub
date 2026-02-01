using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Requirement for role-based authorization
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public string[] Roles { get; }

    public RoleRequirement(params string[] roles)
    {
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }
}
