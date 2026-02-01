using IdentityHub.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.API.Extensions;

/// <summary>
/// Extension methods for authorization configuration
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Configure authorization policies
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            // Permission-based policies
            .AddPolicy("CanManageUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.manage")))
            .AddPolicy("CanViewUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.read")))
            .AddPolicy("CanDeleteUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.delete")))
            .AddPolicy("CanInviteUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.invite")))

            // Document policies
            .AddPolicy("CanManageDocuments", policy =>
                policy.Requirements.Add(new PermissionRequirement("documents.manage")))
            .AddPolicy("CanCreateDocuments", policy =>
                policy.Requirements.Add(new PermissionRequirement("documents.create")))
            .AddPolicy("CanDeleteDocuments", policy =>
                policy.Requirements.Add(new PermissionRequirement("documents.delete")))

            // Ticket policies
            .AddPolicy("CanManageTickets", policy =>
                policy.Requirements.Add(new PermissionRequirement("tickets.manage")))
            .AddPolicy("CanAssignTickets", policy =>
                policy.Requirements.Add(new PermissionRequirement("tickets.assign")))
            .AddPolicy("CanViewAllTickets", policy =>
                policy.Requirements.Add(new PermissionRequirement("tickets.view.all")))

            // Billing policies
            .AddPolicy("CanManageBilling", policy =>
                policy.Requirements.Add(new PermissionRequirement("billing.manage")))
            .AddPolicy("CanViewBilling", policy =>
                policy.Requirements.Add(new PermissionRequirement("billing.view")))

            // Audit policies
            .AddPolicy("CanViewAudit", policy =>
                policy.Requirements.Add(new PermissionRequirement("audit.view")))

            // Settings policies
            .AddPolicy("CanManageSettings", policy =>
                policy.Requirements.Add(new PermissionRequirement("settings.manage")))

            // Role-based policies
            .AddPolicy("RequireAdmin", policy =>
                policy.Requirements.Add(new RoleRequirement("Admin")))
            .AddPolicy("RequireSupportAgent", policy =>
                policy.Requirements.Add(new RoleRequirement("SupportAgent")))
            .AddPolicy("RequireAdminOrAgent", policy =>
                policy.Requirements.Add(new RoleRequirement("Admin", "SupportAgent")));

        return services;
    }
}
