namespace IdentityHub.Contracts.DTOs.Users.Requests;

/// <summary>
/// DTO for creating a new user.
/// </summary>
public class CreateUserRequest
{
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? Mail { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? MobilePhone { get; set; }
    public string? OfficeLocation { get; set; }
    public bool? AccountEnabled { get; set; }
    public List<string>? BusinessPhones { get; set; }
    public List<string> RoleIds { get; set; } = [];
}
