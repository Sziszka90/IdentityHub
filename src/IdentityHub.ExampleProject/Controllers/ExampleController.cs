using IdentityHub.Client;
using IdentityHub.Client.Authorization;
using IdentityHub.ExampleProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityHub.ExampleProject.Controllers;

[ApiController]
[Route("api/example")]
public class ExampleController : ControllerBase
{
    private readonly PermissionCheckProbe _probe;
    private readonly IdentityHubClientOptions _clientOptions;

    public ExampleController(
        PermissionCheckProbe probe,
        IOptions<IdentityHubClientOptions> clientOptions)
    {
        _probe = probe;
        _clientOptions = clientOptions.Value;
    }

    [HttpGet("fixed")]
    [Authorize]
    [RequirePermission("example.read")]
    public IActionResult GetFixedValue()
    {
        return Ok(new
        {
            value = "fixed-value",
            message = "Permission attribute and IdentityHub client authorization succeeded."
        });
    }

    [HttpGet("cache-stats")]
    [AllowAnonymous]
    public IActionResult GetCacheStats()
    {
        return Ok(new
        {
            permissionCheckCalls = _probe.PermissionCheckCalls,
            cacheProvider = _clientOptions.CacheProvider.ToString(),
            baseUrl = _clientOptions.BaseUrl,
            ttlSeconds = _clientOptions.PermissionCheckCacheSeconds
        });
    }

    [HttpPost("cache-stats/reset")]
    [AllowAnonymous]
    public IActionResult ResetCacheStats()
    {
        _probe.Reset();
        return NoContent();
    }
}
