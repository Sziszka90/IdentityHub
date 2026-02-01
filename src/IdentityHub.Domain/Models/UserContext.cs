namespace IdentityHub.Domain.Models;

/// <summary>
/// Represents the authenticated user's identity context
/// </summary>
public class UserContext
{
    /// <summary>
    /// Unique identifier for the user (Object ID from Entra ID)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID from which the user authenticated
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Roles assigned to the user in IdentityHub
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// Granular permissions granted to the user (resolved from roles)
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// Check if user has a specific permission (supports wildcards)
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
        {
            return false;
        }

        // Check exact match
        if (Permissions.Contains(permission))
        {
            return true;
        }

        // Check wildcard permissions (e.g., "users.*" matches "users.read")
        string[] parts = permission.Split('.');
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            string wildcardPattern = string.Join(".", parts.Take(i)) + ".*";
            if (Permissions.Contains(wildcardPattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Azure AD groups the user belongs to
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Raw claims from the JWT token
    /// </summary>
    public Dictionary<string, string> Claims { get; set; } = [];

    /// <summary>
    /// Timestamp when the user context was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user has been authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }
}
