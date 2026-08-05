using System;
using System.Linq;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SettingsAiFeatureToggle : IAiFeatureToggle
{
    private readonly CheckEngineAiOptions _options;

    public SettingsAiFeatureToggle(CheckEngineAiOptions? options = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public bool IsEnabled(string featureKey)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            return false;

        return _options.EnabledFeatures.Any(x => string.Equals(x, featureKey, StringComparison.OrdinalIgnoreCase));
    }
}
