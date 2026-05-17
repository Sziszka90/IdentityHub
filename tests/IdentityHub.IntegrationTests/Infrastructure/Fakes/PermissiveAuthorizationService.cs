using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.IntegrationTests.Infrastructure.Fakes;

/// <summary>
/// Replaces <see cref="IAuthorizationService"/> in integration tests.
/// Always returns success so that [Authorize] and [RequirePermission] attributes
/// never block test requests; actual business logic is what's under test.
/// </summary>
public class PermissiveAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
        => Task.FromResult(AuthorizationResult.Success());

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName)
        => Task.FromResult(AuthorizationResult.Success());
}
