using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Paymob;

/// <summary>
/// Settings for the Paymob (Accept) redirect payment method. Credentials are configured per
/// deployment and never committed.
/// </summary>
public class PaymobPaymentSettings : ISettings
{
    /// <summary>Paymob API key used to authenticate and obtain auth tokens.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Integration id for the card payment integration.</summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>Iframe id used to build the hosted checkout URL.</summary>
    public string IframeId { get; set; } = string.Empty;

    /// <summary>HMAC secret used to verify transaction-processed callbacks.</summary>
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>When true, the plugin does not contact Paymob and is inert (safe default).</summary>
    public bool UseSandbox { get; set; } = true;

    public bool AdditionalFeePercentage { get; set; }

    public decimal AdditionalFee { get; set; }
}
