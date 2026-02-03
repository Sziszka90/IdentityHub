using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

        services.Configure<RedisCacheOptions>(
            configuration.GetSection(RedisCacheOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IGraphService, GraphService>();
        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }

    /// <summary>
    /// Configure Redis distributed caching
    /// </summary>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisCacheOptions = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>();

        if (redisCacheOptions?.Enabled == true && !string.IsNullOrEmpty(redisCacheOptions.ConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisCacheOptions.ConnectionString;
                options.InstanceName = "IdentityHub:";
            });
        }
        else
        {
            // Add a no-op distributed cache if Redis is not configured
            services.AddDistributedMemoryCache();
        }

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
