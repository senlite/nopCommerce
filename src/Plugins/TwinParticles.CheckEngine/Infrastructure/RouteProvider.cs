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

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.FitmentEvaluate",
            pattern: "check-engine/fitment/evaluate",
            defaults: new { controller = "Fitment", action = "Evaluate" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.FitmentAdmin",
            pattern: "Admin/CheckEngine/FitmentAdmin/{action}",
            defaults: new { controller = "FitmentAdmin", action = "Queue" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchQuery",
            pattern: "check-engine/search/query",
            defaults: new { controller = "Search", action = "Query" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchAdmin",
            pattern: "Admin/CheckEngine/SearchAdmin/{action}",
            defaults: new { controller = "SearchAdmin", action = "Rebuild" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Garage",
            pattern: "check-engine/garage/{action}",
            defaults: new { controller = "Garage", action = "Current" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.GarageAdmin",
            pattern: "Admin/CheckEngine/GarageAdmin/{action}",
            defaults: new { controller = "GarageAdmin", action = "CustomerGarage" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ImageAdmin",
            pattern: "Admin/CheckEngine/ImageAdmin/{action}",
            defaults: new { controller = "ImageAdmin", action = "Replace" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.L10nPreview",
            pattern: "check-engine/l10n/preview",
            defaults: new { controller = "L10n", action = "Preview" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoAdmin",
            pattern: "Admin/CheckEngine/SeoAdmin/{action}",
            defaults: new { controller = "SeoAdmin", action = "Sitemap" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ErpAdmin",
            pattern: "Admin/CheckEngine/ErpAdmin/{action}",
            defaults: new { controller = "ErpAdmin", action = "Reconcile" });
    }

    public int Priority => 0;
}
