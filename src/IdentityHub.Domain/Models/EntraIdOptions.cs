namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration options for Azure Entra ID authentication
/// </summary>
public class EntraIdOptions
{
    public const string SectionName = "EntraId";

    /// <summary>
    /// Azure AD Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (Client) ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret (not used with Managed Identity)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Azure AD Instance (e.g., https://login.microsoftonline.com/)
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// API Audience (typically api://{ClientId})
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use Managed Identity for Graph API calls
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Scopes required for Microsoft Graph API
    /// </summary>
    public string[] GraphApiScopes { get; set; } = ["https://graph.microsoft.com/.default"];

    /// <summary>
    /// Authority URL for token validation
    /// </summary>
    public string Authority => $"{Instance}{TenantId}";
}
