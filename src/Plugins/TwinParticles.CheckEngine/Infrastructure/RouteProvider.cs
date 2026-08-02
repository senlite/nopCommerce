using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace TwinParticles.CheckEngine.Infrastructure;

public sealed class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Configure",
            pattern: "Admin/CheckEngine/Configure",
            defaults: new { controller = "CheckEngine", action = "Configure" });
    }

    public int Priority => 0;
}
