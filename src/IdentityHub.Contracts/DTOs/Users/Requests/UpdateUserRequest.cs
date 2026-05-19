namespace IdentityHub.Contracts.DTOs.Users.Requests;

/// <summary>
/// DTO for updating a user.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class UpdateUserRequest
{
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string MailNickname { get; set; } = string.Empty;

    [EmailAddress]
    public string? Mail { get; set; }

    [Required]
    [EmailAddress]
    public string UserPrincipalName { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public bool AccountEnabled { get; set; } = true;
}
