using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Data;
using TwinParticles.CheckEngine.Application.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure;

/// <summary>
/// FR-1310 application isolation: resolve tenant once per request from host or X-CE-Tenant-Slug.
/// API-key authentication rebinds after this when a public key is presented.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantResolver resolver)
    {
        if (DataSettingsManager.IsDatabaseInstalled())
        {
            try
            {
                context.Request.Headers.TryGetValue("X-CE-Tenant-Slug", out var slug);
                await resolver.ResolveAsync(new TenantResolutionRequest
                {
                    Hostname = context.Request.Host.Host,
                    Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.ToString()
                }, context.RequestAborted);
            }
            catch (Exception)
            {
                // Control-plane tables may not exist until plugin update; leave context unresolved.
            }
        }

        await _next(context);
    }
}
