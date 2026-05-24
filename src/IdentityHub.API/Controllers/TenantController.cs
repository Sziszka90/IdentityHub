using IdentityHub.Application.Interfaces;
using IdentityHub.Contracts.DTOs.Tenants.Responses;
using IdentityHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantContextService _tenantContextService;
    private readonly IUserTenantMappingsRepository _userTenantMappingsRepository;
    private readonly TenantConfigurationOptions _tenantOptions;

    public TenantController(
        ITenantContextService tenantContextService,
        IUserTenantMappingsRepository userTenantMappingsRepository,
        IOptions<TenantConfigurationOptions> tenantOptions)
    {
        _tenantContextService = tenantContextService;
        _userTenantMappingsRepository = userTenantMappingsRepository;
        _tenantOptions = tenantOptions.Value;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    public IActionResult GetCurrentTenant()
    {
        var tenantContext = _tenantContextService.GetTenantContext();

        return Ok(new TenantResponse
        {
            TenantId = tenantContext.TenantId,
            UserId = tenantContext.UserId,
            IsValid = tenantContext.IsValid
        });
    }

    [HttpGet("configuration")]
    [ProducesResponseType(typeof(TenantConfigurationResponse), StatusCodes.Status200OK)]
    public IActionResult GetTenantConfiguration()
    {
        var tenantContext = _tenantContextService.GetTenantContext();
        var isAllowed = _tenantOptions.AllowedTenantIds.Count == 0
            || _tenantOptions.AllowedTenantIds.Contains(tenantContext.TenantId, StringComparer.OrdinalIgnoreCase);

        return Ok(new TenantConfigurationResponse
        {
            HeaderName = _tenantOptions.HeaderName,
            CurrentTenantId = tenantContext.TenantId,
            IsCurrentTenantAllowed = isAllowed,
            AllowedTenantIds = _tenantOptions.AllowedTenantIds
        });
    }
}
