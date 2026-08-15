using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SettingsAiSpendPolicy : IAiSpendPolicy
{
    private readonly CheckEngineAiOptions _options;

    public SettingsAiSpendPolicy(CheckEngineAiOptions? options = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public bool DisclosureAcknowledged => _options.DisclosureAcknowledged;

    public int ResolveDailyCeiling(string featureKey) => _options.ResolveDailyCeiling(featureKey);
}
