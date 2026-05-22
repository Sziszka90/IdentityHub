using IdentityHub.ExampleProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.ExampleProject.Controllers;

[ApiController]
[Route("mock-identityhub/api/authorization")]
public class MockIdentityHubController : ControllerBase
{
    private readonly PermissionCheckProbe _probe;

    public MockIdentityHubController(PermissionCheckProbe probe)
    {
        _probe = probe;
    }

    [HttpPost("check")]
    [AllowAnonymous]
    public IActionResult CheckPermission([FromBody] PermissionCheckRequest request)
    {
        var callCount = _probe.IncrementPermissionCheckCalls();
        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : string.Empty;

        var allowed = token == "allow-token" && request.Permission == "example.read";

        return Ok(new
        {
            allowed,
            reason = allowed ? "Mock permission granted" : "Mock permission denied",
            permission = request.Permission,
            userId = "example-user",
            callCount
        });
    }

    public class PermissionCheckRequest
    {
        public string Permission { get; set; } = string.Empty;
    }
}
