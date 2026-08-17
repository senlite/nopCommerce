using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class MarketplaceOnboardingOptions : IMarketplaceOnboardingPolicy
{
    public static MarketplaceOnboardingOptions Current { get; } = new();

    public bool ApplicationsOpen { get; set; }

    public string CurrentAgreementVersion { get; set; } = VendorAgreementVersions.Default;
}
