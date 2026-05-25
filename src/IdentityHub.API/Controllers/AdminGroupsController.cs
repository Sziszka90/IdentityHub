using IdentityHub.Application.Interfaces;
using IdentityHub.Client.Authorization;
using IdentityHub.Contracts.DTOs.Groups.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/admin/groups")]
public class AdminGroupsController : ControllerBase
{
    private readonly IGraphService _graphService;

    public AdminGroupsController(IGraphService graphService)
    {
        _graphService = graphService;
    }

    [HttpGet]
    [RequirePermission("groups.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups([FromQuery] string? displayName = null)
    {
        var groups = await _graphService.QueryGroupsAsync(displayName);

        var groupResponses = groups.Select(group => new GroupResponse
        {
            Id = group.Id ?? string.Empty,
            DisplayName = group.DisplayName ?? string.Empty,
            MailNickname = group.MailNickname,
            Mail = group.Mail,
            Description = group.Description,
            SecurityEnabled = group.SecurityEnabled
        }).ToList();

        return Ok(new { count = groupResponses.Count, groups = groupResponses });
    }
}
