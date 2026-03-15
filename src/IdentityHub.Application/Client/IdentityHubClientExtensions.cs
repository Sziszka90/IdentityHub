using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.Application.Client;

/// <summary>
/// Registration helpers for external apps that consume the IdentityHub.Authorization NuGet
/// and want to read role/permission/policy config from the central IdentityHub.API service.
/// </summary>
public static class IdentityHubClientExtensions
{
    /// <summary>
    /// Registers <see cref="IdentityHubClient"/> as the <see cref="IIdentityHubClient"/>
    /// typed HTTP client. Once registered, <see cref="IdentityHub.Application.Services.PermissionService"/>
    /// will use it to resolve roles and permissions from the central IdentityHub.API.
    ///
    /// <code>
    /// // YourApp/Program.cs
    /// builder.Services.AddIdentityHubClient(builder.Configuration);
    /// builder.Services.AddAuthorizationPolicies(builder.Configuration);
    /// </code>
    ///
    /// Required appsettings section:
    /// <code>
    /// "IdentityHubClient": {
    ///   "BaseUrl": "https://identityhub.example.com",
    ///   "ApiKey": "optional-bearer-token",
    ///   "CacheSeconds": 300
    /// }
    /// </code>
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

        // IdentityHubClient extends HttpClient and self-configures from IOptions in its constructor.
        // Registered as a singleton so the underlying TCP connections are reused across requests.
        services.AddSingleton<IdentityHubClient>();
        services.AddSingleton<IIdentityHubClient>(sp => sp.GetRequiredService<IdentityHubClient>());

        return services;
    }
}
