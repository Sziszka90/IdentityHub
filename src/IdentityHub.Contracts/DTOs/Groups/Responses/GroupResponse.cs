namespace IdentityHub.Contracts.DTOs.Groups.Responses;

public class GroupResponse
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? MailNickname { get; set; }
    public string? Mail { get; set; }
    public string? Description { get; set; }
    public bool? SecurityEnabled { get; set; }
}
