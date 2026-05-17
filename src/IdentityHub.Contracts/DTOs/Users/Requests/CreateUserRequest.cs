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

    [StringLength(50)]
    public string? GivenName { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

    [Required]
    [EmailAddress]
    public string UserPrincipalName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Mail { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [Phone]
    public string? MobilePhone { get; set; }

    [StringLength(100)]
    public string? OfficeLocation { get; set; }

    public bool? AccountEnabled { get; set; }

    public List<string>? BusinessPhones { get; set; }

    [MinLength(1)]
    public List<string> RoleIds { get; set; } = new();
}
