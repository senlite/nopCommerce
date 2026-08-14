using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.Bosta;

public class BostaShippingSettings : ISettings
{
    /// <summary>Bosta API key (kept as a secret; never committed).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Display name of the shipping option shown at checkout.</summary>
    public string OptionName { get; set; } = "Bosta";

    /// <summary>When true, rates are computed locally and no Bosta API call is attempted.</summary>
    public bool UseSandbox { get; set; } = true;
}
