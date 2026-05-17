namespace IdentityHub.Contracts.DTOs.Users.Requests;

/// <summary>
/// DTO for updating a user.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class UpdateUserRequest
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(50)]
    public string? GivenName { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

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

    public List<string>? Roles { get; set; }

    public List<string>? Groups { get; set; }
}
