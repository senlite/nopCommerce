using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Bosta.Models;

public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Shipping.Bosta.Fields.ApiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Shipping.Bosta.Fields.OptionName")]
    public string OptionName { get; set; } = "Bosta";

    [NopResourceDisplayName("Plugins.Shipping.Bosta.Fields.UseSandbox")]
    public bool UseSandbox { get; set; } = true;
}
