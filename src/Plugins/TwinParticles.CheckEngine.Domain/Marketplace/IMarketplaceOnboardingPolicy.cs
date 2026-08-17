namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IMarketplaceOnboardingPolicy
{
    bool ApplicationsOpen { get; }

    string CurrentAgreementVersion { get; }
}
