namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiSpendPolicy
{
    bool DisclosureAcknowledged { get; }

    int GlobalDailyCeiling { get; }

    decimal TokenCostPer1KUsd { get; }

    int ResolveDailyCeiling(string featureKey);
}
