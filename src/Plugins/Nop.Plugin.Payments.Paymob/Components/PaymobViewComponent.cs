using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Paymob.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Paymob.Components;

public class PaymobViewComponent : NopViewComponent
{
    private readonly ILocalizationService _localizationService;

    public PaymobViewComponent(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetResourceAsync("Plugins.Payments.Paymob.PaymentMethodDescription")
        };

        return View("~/Plugins/Payments.Paymob/Views/PaymentInfo.cshtml", model);
    }
}
