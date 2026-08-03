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

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VehicleAdmin",
            pattern: "Admin/CheckEngine/VehicleAdmin/{action}",
            defaults: new { controller = "VehicleAdmin", action = "Makes" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VinDecode",
            pattern: "check-engine/vin/decode",
            defaults: new { controller = "Vin", action = "Decode" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.OemAdmin",
            pattern: "Admin/CheckEngine/OemAdmin/{action}",
            defaults: new { controller = "OemAdmin", action = "Manufacturers" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.OemResolve",
            pattern: "check-engine/oem/resolve",
            defaults: new { controller = "Oem", action = "Resolve" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ImportAdmin",
            pattern: "Admin/CheckEngine/ImportAdmin/{action}",
            defaults: new { controller = "ImportAdmin", action = "Run" });
    }

    public int Priority => 0;
}
