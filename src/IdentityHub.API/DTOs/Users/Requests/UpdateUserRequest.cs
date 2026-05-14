namespace IdentityHub.API.DTOs.Users.Requests
{
    /// <summary>
    /// DTO for updating a user.
    /// </summary>
    public class UpdateUserRequest
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? Mail { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? MobilePhone { get; set; }
        public string? OfficeLocation { get; set; }
        public bool? AccountEnabled { get; set; }
        public List<string>? BusinessPhones { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? Groups { get; set; }
    }
}
