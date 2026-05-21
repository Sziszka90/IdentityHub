namespace IdentityHub.Contracts.DTOs.Users.Requests;

/// <summary>
/// DTO for updating a user.
/// </summary>
using System.ComponentModel.DataAnnotations;

public class UpdateUserRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public bool AccountEnabled { get; set; } = true;

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(20)]
    public string? MobilePhone { get; set; }

    [StringLength(50)]
    public string? OfficeLocation { get; set; }

    public List<string> RoleIds { get; set;} = [];
}
