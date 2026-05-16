namespace IdentityHub.Client;

/// <summary>
/// Configuration options for the IdentityHub HTTP client.
/// </summary>
public class IdentityHubClientOptions
{
    public const string SectionName = "IdentityHubClient";

    /// <summary>Base URL of the IdentityHub.API service, e.g. https://identityhub.example.com</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional bearer token for machine-to-machine calls.
    /// Leave empty if the outgoing request already carries a user token.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Cache TTL in seconds for the authorization config snapshot (default: 300).</summary>
    public int CacheSeconds { get; set; } = 300;
}
