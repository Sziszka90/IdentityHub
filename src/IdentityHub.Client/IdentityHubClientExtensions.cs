using IdentityHub.Client.Authorization;
using IdentityHub.Client.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IdentityHub.Client;

/// <summary>
/// Registration helpers for external apps that consume the IdentityHub.Client NuGet
/// and want to read role/permission/policy config from the central IdentityHub.API service.
/// </summary>
public static class IdentityHubClientExtensions
{
    /// <summary>
    /// Registers <see cref="IdentityHubClient"/> as the <see cref="IIdentityHubClient"/>
    /// typed HTTP client. Once registered, consumers can resolve roles and permissions from
    /// the central IdentityHub.API.
    ///
    /// Optionally call <see cref="AddIdentityHubAuthorization"/> to also enable the
    /// <see cref="RequirePermissionAttribute"/> on your controllers.
    /// </summary>
    public static IServiceCollection AddIdentityHubClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(IdentityHubClientOptions.SectionName)
            .Get<IdentityHubClientOptions>()
            ?? throw new InvalidOperationException(
                $"Missing configuration section '{IdentityHubClientOptions.SectionName}'. " +
                "Add IdentityHubClient:BaseUrl to your appsettings.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException("IdentityHubClient:BaseUrl must be set in appsettings.");

        services.Configure<IdentityHubClientOptions>(
            configuration.GetSection(IdentityHubClientOptions.SectionName));

        services.TryAddSingleton<IIdentityHubCacheStore>(sp =>
        {
            var clientOptions = sp.GetRequiredService<IOptions<IdentityHubClientOptions>>().Value;

            if (clientOptions.CacheProvider == IdentityHubCacheProvider.Distributed)
            {
                return new DistributedIdentityHubCacheStore(sp.GetRequiredService<IDistributedCache>());
            }

            return new MemoryIdentityHubCacheStore(sp.GetRequiredService<IMemoryCache>());
        });

        if (options.CacheProvider == IdentityHubCacheProvider.Distributed)
        {
            if (string.IsNullOrWhiteSpace(options.RedisConnectionString))
            {
                throw new InvalidOperationException(
                    "IdentityHubClient:RedisConnectionString must be set when CacheProvider is Distributed.");
            }

            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = options.RedisConnectionString;
                redisOptions.InstanceName = options.RedisInstanceName;
            });
        }
        else
        {
            services.AddMemoryCache();
        }

        services.AddSingleton<IdentityHubClient>();
        services.AddSingleton<IIdentityHubClient>(sp => sp.GetRequiredService<IdentityHubClient>());

        return services;
    }

    /// <summary>
    /// Adds the IdentityHub permission-based authorization integration so that
    /// <see cref="RequirePermissionAttribute"/> works on your controllers.
    ///
    /// Call this after <see cref="AddIdentityHubClient"/> and <c>AddAuthorization()</c>:
    /// <code>
    /// builder.Services
    ///     .AddIdentityHubClient(config)
    ///     .AddIdentityHubAuthorization();
    /// </code>
    /// </summary>
    public static IServiceCollection AddIdentityHubAuthorization(
        this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
