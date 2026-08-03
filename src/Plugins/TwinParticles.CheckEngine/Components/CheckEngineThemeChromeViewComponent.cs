using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace TwinParticles.CheckEngine.Components;

public sealed class CheckEngineThemeChromeViewComponent : NopViewComponent
{
    public IViewComponentResult Invoke(string widgetZone, object? additionalData)
    {
        var model = new ThemeChromeModel
        {
            WidgetZone = widgetZone
        };

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Shared/Components/CheckEngineThemeChrome/Default.cshtml", model);
    }

    public sealed class ThemeChromeModel
    {
        public string WidgetZone { get; init; } = string.Empty;
    }
}
