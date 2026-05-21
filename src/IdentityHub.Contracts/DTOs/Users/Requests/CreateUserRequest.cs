namespace IdentityHub.Contracts.DTOs.Users.Requests;

/// <summary>
/// DTO for creating a new user.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class CreateUserRequest
{
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string UserPrincipalName { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string MailNickname { get; set; } = string.Empty;

    [EmailAddress]
    public string? Mail { get; set; }

    public bool AccountEnabled { get; set; } = true;

    public List<string> RoleIds { get; set; } = new();
    
    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(50)]
    public string? OfficeLocation { get; set; }
}
