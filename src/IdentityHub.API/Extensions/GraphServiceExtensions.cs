using Azure.Identity;
using IdentityHub.Domain.Models;
using Microsoft.Graph;

namespace IdentityHub.API.Extensions;

/// <summary>
/// Extension methods for configuring Microsoft Graph API
/// </summary>
public static class GraphServiceExtensions
{
    /// <summary>
    /// Add Microsoft Graph API client with support for Managed Identity or client secret
    /// </summary>
    public static IServiceCollection AddGraphApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entraIdOptions = configuration.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>();

        if (entraIdOptions is null)
        {
            return services;
        }

        services.AddScoped(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GraphServiceClient>>();

            try
            {
                if (entraIdOptions.UseManagedIdentity)
                {
                    logger.LogInformation("Configuring Graph API with Managed Identity");

                    var credential = new DefaultAzureCredential();

                    return new GraphServiceClient(credential, entraIdOptions.GraphApiScopes);
                }
                else
                {
                    logger.LogInformation("Configuring Graph API with Client Secret");

                    if (string.IsNullOrEmpty(entraIdOptions.ClientSecret))
                    {
                        logger.LogWarning("Client secret is not configured for Graph API");
                        return null!;
                    }

                    var credential = new ClientSecretCredential(
                        entraIdOptions.TenantId,
                        entraIdOptions.ClientId,
                        entraIdOptions.ClientSecret);

                    return new GraphServiceClient(credential, entraIdOptions.GraphApiScopes);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create Graph API client");
                return null!;
            }
        });

        return services;
    }
}
