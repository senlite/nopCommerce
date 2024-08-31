using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CashOnDelivery.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CashOnDelivery.Components;

public class CashOnDeliveryViewComponent : NopViewComponent
{
    protected readonly CashOnDeliveryPaymentSettings _CashOnDeliveryPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    public CashOnDeliveryViewComponent(CashOnDeliveryPaymentSettings CashOnDeliveryPaymentSettings,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _CashOnDeliveryPaymentSettings = CashOnDeliveryPaymentSettings;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();

        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetLocalizedSettingAsync(_CashOnDeliveryPaymentSettings,
                x => x.DescriptionText, (await _workContext.GetWorkingLanguageAsync()).Id, store.Id)
        };

        return View("~/Plugins/Payments.CashOnDelivery/Views/PaymentInfo.cshtml", model);
    }
}