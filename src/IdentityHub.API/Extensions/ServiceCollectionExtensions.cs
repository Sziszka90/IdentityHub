using IdentityHub.API.Authorization;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Models;
using IdentityHub.Infrastructure.Data;
using IdentityHub.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;


namespace IdentityHub.API.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configure authentication with Azure Entra ID
    /// </summary>
    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entraIdOptions = configuration.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>()
            ?? throw new InvalidOperationException("EntraId configuration is missing");

        services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                configuration.Bind("EntraId", options);
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
            },
            options =>
            {
                configuration.Bind("EntraId", options);
            });

        return services;
    }

    /// <summary>
    /// Register application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RolePermissionOptions>(
            configuration.GetSection(RolePermissionOptions.SectionName));
        services.Configure<TenantConfigurationOptions>(
            configuration.GetSection(TenantConfigurationOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, RequirePermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
        services.AddAuthorization();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IGraphService, GraphService>();

        return services;
    }

    /// <summary>
    /// Configure the authorization database (SQL Server + EF Core)
    /// </summary>
    public static IServiceCollection AddAuthorizationDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthorizationDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AuthorizationDb is not configured");

        services.AddDbContext<IdentityHubDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>()));

        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IPermissionsRepository, PermissionsRepository>();

        return services;
    }

    /// <summary>
    /// Configure CORS policy
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        return services;
    }
}
