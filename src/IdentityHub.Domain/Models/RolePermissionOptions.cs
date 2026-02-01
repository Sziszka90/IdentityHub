namespace IdentityHub.Domain.Models;

/// <summary>
/// Configuration for role-to-permission mappings
/// </summary>
public class RolePermissionOptions
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Maps Entra ID groups to application roles
    /// Example: { "Global-Admins": "Admin", "Support-Team": "SupportAgent" }
    /// </summary>
    public Dictionary<string, string> GroupToRoleMapping { get; set; } = new();

    /// <summary>
    /// Maps application roles to their permissions
    /// Example: { "Admin": ["users.*", "documents.*"], "User": ["documents.read"] }
    /// </summary>
    public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
}
