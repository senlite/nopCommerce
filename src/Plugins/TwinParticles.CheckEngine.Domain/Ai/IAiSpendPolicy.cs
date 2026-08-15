namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiSpendPolicy
{
    bool DisclosureAcknowledged { get; }

    int ResolveDailyCeiling(string featureKey);
}
