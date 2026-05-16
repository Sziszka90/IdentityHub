
namespace IdentityHub.Client;

/// <summary>
/// Registration helpers for external apps that consume the IdentityHub.Client NuGet
/// and want to read role/permission/policy config from the central IdentityHub.API service.
/// </summary>
public static class IdentityHubClientExtensions
{
    /// <summary>
    /// Registers <see cref="IdentityHubClient"/> as the <see cref="IIdentityHubClient"/>
    /// typed HTTP client. Once registered, consumers can resolve roles and permissions from the central IdentityHub.API.
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

        services.AddSingleton<IdentityHubClient>();
        services.AddSingleton<IIdentityHubClient>(sp => sp.GetRequiredService<IdentityHubClient>());

        return services;
    }
}
