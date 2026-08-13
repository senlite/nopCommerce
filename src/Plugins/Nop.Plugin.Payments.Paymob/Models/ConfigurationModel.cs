using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Paymob.Models;

public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Payments.Paymob.Fields.ApiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Payments.Paymob.Fields.IntegrationId")]
    public string IntegrationId { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Payments.Paymob.Fields.IframeId")]
    public string IframeId { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Payments.Paymob.Fields.HmacSecret")]
    public string HmacSecret { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Payments.Paymob.Fields.UseSandbox")]
    public bool UseSandbox { get; set; } = true;
}
