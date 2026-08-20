using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using TwinParticles.CheckEngine.Application.Licensing;

namespace TwinParticles.CheckEngine.Components;

/// <summary>
/// Persistent admin warning when licence read-only mode blocks mutations (FR-981 / ADR-009).
/// </summary>
public sealed class LicenceReadOnlyBannerViewComponent : NopViewComponent
{
    private readonly CheckEngineLicenceGate _licenceGate;

    public LicenceReadOnlyBannerViewComponent(CheckEngineLicenceGate licenceGate)
    {
        _licenceGate = licenceGate;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (await _licenceGate.AllowsAdminWriteAsync(HttpContext.RequestAborted))
            return Content(string.Empty);

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Shared/Components/LicenceReadOnlyBanner/Default.cshtml");
    }
}
