using Azure.Identity;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Extensions.Logging;

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

        if (entraIdOptions == null)
        {
            // No Entra ID configuration - Graph API will not be available
            services.AddSingleton<GraphServiceClient?>((_) => null);
            return services;
        }

        services.AddSingleton<GraphServiceClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GraphServiceClient>>();

            try
            {
                if (entraIdOptions.UseManagedIdentity)
                {
                    logger.LogInformation("Configuring Graph API with Managed Identity");

                    // Use Managed Identity (no secrets needed!)
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

                    // Use client secret
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
