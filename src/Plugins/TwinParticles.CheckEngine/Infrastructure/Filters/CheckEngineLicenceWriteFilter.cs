using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using TwinParticles.CheckEngine.Application.Licensing;

namespace TwinParticles.CheckEngine.Infrastructure.Filters;

/// <summary>
/// Blocks Check Engine admin mutations when the licence is in read-only mode (FR-981 / ADR-009).
/// Storefront routes are never affected.
/// </summary>
public sealed class CheckEngineLicenceWriteFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            await next();
            return;
        }

        // ADR-009: only Admin mutations are gated. Missing area = storefront; never block it.
        if (!context.RouteData.Values.TryGetValue("area", out var area) ||
            !string.Equals(area?.ToString(), AreaNames.ADMIN, StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var controllerType = context.Controller.GetType();
        var ns = controllerType.Namespace;
        if (ns is null || !ns.StartsWith("TwinParticles.CheckEngine.Controllers", StringComparison.Ordinal))
        {
            await next();
            return;
        }

        // Licence activation and diagnostics must remain reachable while read-only.
        if (controllerType.Name is "DiagnosticsAdminController" or "UninstallAdminController")
        {
            await next();
            return;
        }

        var gate = context.HttpContext.RequestServices.GetService(typeof(CheckEngineLicenceGate)) as CheckEngineLicenceGate;
        if (gate is not null && !await gate.AllowsAdminWriteAsync(context.HttpContext.RequestAborted))
        {
            context.Result = new JsonResult(new { reasonCode = "licence.read_only" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
