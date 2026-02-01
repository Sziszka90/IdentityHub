using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Authorization requirement to ensure user belongs to valid tenant
/// </summary>
public class TenantRequirement : IAuthorizationRequirement
{
    public TenantRequirement()
    {
    }
}
