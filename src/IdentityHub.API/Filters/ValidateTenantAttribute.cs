using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdentityHub.Application.Interfaces;

namespace IdentityHub.API.Filters;

/// <summary>
/// Action filter to validate tenant context on controller actions
/// </summary>
public class ValidateTenantAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var tenantService = context.HttpContext.RequestServices
            .GetService<ITenantContextService>();

        if (tenantService is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var tenantContext = tenantService.GetTenantContext();

        if (!tenantService.ValidateTenantContext(tenantContext))
        {
            context.Result = new ObjectResult(new { error = "Invalid tenant context" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
