using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace TwinParticles.CheckEngine.Infrastructure;

public sealed class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // Admin controllers are decorated with [Area("Admin")]. Conventional routing only binds to an
        // area controller when the route supplies the matching area value, so every admin route below
        // sets area = Admin. Without it these endpoints 404 (the CheckEngine controller's fixed routes
        // happened to work only because nopCommerce's global {area}/{controller}/{action} route matched
        // the "CheckEngine" URL segment as the controller name).
        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Configure",
            pattern: "Admin/CheckEngine/Configure",
            defaults: new { area = AreaNames.ADMIN, controller = "CheckEngine", action = "Configure" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Dashboard",
            pattern: "Admin/CheckEngine/Dashboard",
            defaults: new { area = AreaNames.ADMIN, controller = "CheckEngine", action = "Dashboard" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoVehicleAr",
            pattern: "ar/vehicles/config-{vehicleConfigurationId:int}",
            defaults: new { controller = "SeoLanding", action = "Vehicle", locale = "ar" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoVehicleEn",
            pattern: "vehicles/config-{vehicleConfigurationId:int}",
            defaults: new { controller = "SeoLanding", action = "Vehicle", locale = "en" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoPartVehicleAr",
            pattern: "ar/parts/product-{productId:int}/for/config-{vehicleConfigurationId:int}",
            defaults: new { controller = "SeoLanding", action = "PartForVehicle", locale = "ar" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoPartVehicleEn",
            pattern: "parts/product-{productId:int}/for/config-{vehicleConfigurationId:int}",
            defaults: new { controller = "SeoLanding", action = "PartForVehicle", locale = "en" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VehicleAdmin",
            pattern: "Admin/CheckEngine/VehicleAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "VehicleAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VinDecode",
            pattern: "check-engine/vin/decode",
            defaults: new { controller = "Vin", action = "Decode" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.OemAdmin",
            pattern: "Admin/CheckEngine/OemAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "OemAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.OemResolve",
            pattern: "check-engine/oem/resolve",
            defaults: new { controller = "Oem", action = "Resolve" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ImportAdmin",
            pattern: "Admin/CheckEngine/ImportAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "ImportAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.FitmentEvaluate",
            pattern: "check-engine/fitment/evaluate",
            defaults: new { controller = "Fitment", action = "Evaluate" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.FitmentAdmin",
            pattern: "Admin/CheckEngine/FitmentAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "FitmentAdmin", action = "ClaimsReview" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchQuery",
            pattern: "check-engine/search/query",
            defaults: new { controller = "Search", action = "Query" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchSuggest",
            pattern: "check-engine/search/suggest",
            defaults: new { controller = "Search", action = "Suggest" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchClick",
            pattern: "check-engine/search/click",
            defaults: new { controller = "Search", action = "Click" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchRecommend",
            pattern: "check-engine/search/recommend",
            defaults: new { controller = "Search", action = "Recommend" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SearchAdmin",
            pattern: "Admin/CheckEngine/SearchAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "SearchAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.UninstallAdmin",
            pattern: "Admin/CheckEngine/UninstallAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "UninstallAdmin", action = "Status" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Garage",
            pattern: "check-engine/garage/{action?}",
            defaults: new { controller = "Garage", action = "Current" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.GarageAdmin",
            pattern: "Admin/CheckEngine/GarageAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "GarageAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ImageAdmin",
            pattern: "Admin/CheckEngine/ImageAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "ImageAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.L10nPreview",
            pattern: "check-engine/l10n/preview",
            defaults: new { controller = "L10n", action = "Preview" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.SeoAdmin",
            pattern: "Admin/CheckEngine/SeoAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "SeoAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ErpAdmin",
            pattern: "Admin/CheckEngine/ErpAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "ErpAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Health",
            pattern: "check-engine/health",
            defaults: new { controller = "Health", action = "Get" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ErpWebhook",
            pattern: "check-engine/erp/webhook",
            defaults: new { controller = "ErpWebhook", action = "Receive" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.ReferenceDataAdmin",
            pattern: "Admin/CheckEngine/ReferenceDataAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "ReferenceDataAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.DiagnosticsAdmin",
            pattern: "Admin/CheckEngine/DiagnosticsAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "DiagnosticsAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.AssistantAsk",
            pattern: "check-engine/assistant/ask",
            defaults: new { controller = "Assistant", action = "Ask" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.AiAdmin",
            pattern: "Admin/CheckEngine/AiAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "AiAdmin", action = "Dashboard" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VendorAdmin",
            pattern: "Admin/CheckEngine/VendorAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "VendorAdmin", action = "ReviewBoard" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.PayoutAdmin",
            pattern: "Admin/CheckEngine/PayoutAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "PayoutAdmin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.CommissionAdmin",
            pattern: "Admin/CheckEngine/CommissionAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "CommissionAdmin", action = "Configure" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.VendorApply",
            pattern: "check-engine/vendor/{action?}",
            defaults: new { controller = "Vendor", action = "Apply" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Workshop",
            pattern: "check-engine/workshop/{action?}",
            defaults: new { controller = "Workshop", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Fleet",
            pattern: "check-engine/fleet/{action?}",
            defaults: new { controller = "Fleet", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.Dealer",
            pattern: "check-engine/dealer/{action?}",
            defaults: new { controller = "Dealer", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.TwinParticles.CheckEngine.PortalAdmin",
            pattern: "Admin/CheckEngine/PortalAdmin/{action?}",
            defaults: new { area = AreaNames.ADMIN, controller = "PortalAdmin", action = "Accounts" });
    }

    public int Priority => 0;
}
