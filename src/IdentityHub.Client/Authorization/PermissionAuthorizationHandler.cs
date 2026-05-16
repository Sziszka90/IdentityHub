
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Client.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by calling the central IdentityHub API
/// via <see cref="IIdentityHubClient.CheckPermissionAsync"/>.
/// The current user's bearer token is forwarded so IdentityHub can verify the permission.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IIdentityHubClient _client;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IIdentityHubClient client,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // In ASP.NET Core, context.Resource is always the HttpContext when invoked
        // from the middleware pipeline (MVC, Minimal API, Razor Pages).
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Permission check for '{Permission}' denied: no bearer token in request",
                requirement.Permission);
            context.Fail();
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var result = await _client.CheckPermissionAsync(requirement.Permission, token);

            if (result.Allowed)
            {
                _logger.LogDebug(
                    "Permission '{Permission}' granted for user '{UserId}'",
                    requirement.Permission, result.UserId);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogDebug(
                    "Permission '{Permission}' denied for user '{UserId}': {Reason}",
                    requirement.Permission, result.UserId, result.Reason);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking permission '{Permission}' against IdentityHub API",
                requirement.Permission);
            context.Fail();
        }
    }
}
