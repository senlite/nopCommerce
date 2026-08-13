using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

namespace TwinParticles.CheckEngine.Components;

public sealed class CheckEngineThemeChromeViewComponent : NopViewComponent
{
    public IViewComponentResult Invoke(string widgetZone, object? additionalData)
    {
        var model = new ThemeChromeModel
        {
            WidgetZone = widgetZone,
            ProductId = ResolveProductId(additionalData)
        };

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Shared/Components/CheckEngineThemeChrome/Default.cshtml", model);
    }

    /// <summary>
    /// The product detail widget zones hand us the page's view model. Reading the product id here
    /// keeps the fitment band working regardless of the host theme's markup.
    /// </summary>
    private static int ResolveProductId(object? additionalData) => additionalData switch
    {
        ProductDetailsModel details => details.Id,
        int id when id > 0 => id,
        _ => 0
    };

    public sealed class ThemeChromeModel
    {
        public string WidgetZone { get; init; } = string.Empty;

        /// <summary>Zero when the current zone is not product-scoped.</summary>
        public int ProductId { get; init; }
    }
}
