using System;
using System.Collections.Generic;

using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class CheckEngineAiOptions
{
    public static CheckEngineAiOptions Current { get; set; } = new();

    public AiProviderKind ProviderKind { get; set; } = AiProviderKind.OpenAiCompatible;

    public IReadOnlyCollection<string> EnabledFeatures { get; set; } = Array.Empty<string>();

    public int DailyTokenCeiling { get; set; } = 100_000;

    public Dictionary<string, int> PerFeatureDailyTokenCeilings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o-mini";

    public string AzureDeploymentName { get; set; } = string.Empty;

    public string AzureApiVersion { get; set; } = "2024-02-15-preview";

    public string AnthropicVersion { get; set; } = "2023-06-01";

    public bool DisclosureAcknowledged { get; set; }

    public int ResolveDailyCeiling(string featureKey)
    {
        if (!string.IsNullOrWhiteSpace(featureKey)
            && PerFeatureDailyTokenCeilings.TryGetValue(featureKey, out var perFeature)
            && perFeature > 0)
        {
            return perFeature;
        }

        return DailyTokenCeiling;
    }
}
