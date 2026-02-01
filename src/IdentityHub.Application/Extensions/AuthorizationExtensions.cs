using IdentityHub.Application.Authorization;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.Application.Extensions;

/// <summary>
/// Extension methods for authorization configuration
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Configure authorization policies from configuration
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

        // Load policy configuration
        var policyOptions = configuration
            .GetSection(AuthorizationPoliciesOptions.SectionName)
            .Get<AuthorizationPoliciesOptions>() ?? new AuthorizationPoliciesOptions();

        // Register authorization options configurer
        services.Configure<AuthorizationOptions>(options =>
        {
            // Add permission-based policies from configuration
            foreach (var (policyName, permission) in policyOptions.PermissionPolicies)
            {
                options.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }

            // Add role-based policies from configuration
            foreach (var (policyName, rolesString) in policyOptions.RolePolicies)
            {
                var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                options.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new RoleRequirement(roles)));
            }
        });

        return services;
    }
}
