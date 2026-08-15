using System;
using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Configuration;

public static class CheckEngineAiSettingsSync
{
    public static void Apply(CheckEnginePluginSettings settings)
    {
        var options = CheckEngineAiOptions.Current;
        options.ProviderKind = (AiProviderKind)Math.Max(1, settings.AiProviderKind);
        options.BaseUrl = settings.AiBaseUrl ?? string.Empty;
        options.ApiKey = settings.AiApiKey ?? string.Empty;
        options.Model = string.IsNullOrWhiteSpace(settings.AiModel) ? options.Model : settings.AiModel;
        options.EmbeddingModel = string.IsNullOrWhiteSpace(settings.AiEmbeddingModel) ? options.EmbeddingModel : settings.AiEmbeddingModel;
        options.AzureDeploymentName = settings.AiAzureDeploymentName ?? string.Empty;
        options.DailyTokenCeiling = settings.AiDailyTokenCeiling > 0 ? settings.AiDailyTokenCeiling : options.DailyTokenCeiling;
        options.ResponseCacheTtlMinutes = settings.AiResponseCacheTtlMinutes > 0 ? settings.AiResponseCacheTtlMinutes : options.ResponseCacheTtlMinutes;
        options.DisclosureAcknowledged = settings.AiDisclosureAcknowledged;
        options.EnabledFeatures = ParseEnabledFeatures(settings.AiEnabledFeatures);
        options.PerFeatureDailyTokenCeilings = ParsePerFeatureCeilings(settings.AiPerFeatureDailyTokenCeilings);
    }

    public static void CopyToSettings(CheckEnginePluginSettings settings)
    {
        var options = CheckEngineAiOptions.Current;
        settings.AiProviderKind = (int)options.ProviderKind;
        settings.AiBaseUrl = options.BaseUrl;
        settings.AiApiKey = options.ApiKey;
        settings.AiModel = options.Model;
        settings.AiEmbeddingModel = options.EmbeddingModel;
        settings.AiAzureDeploymentName = options.AzureDeploymentName;
        settings.AiDailyTokenCeiling = options.DailyTokenCeiling;
        settings.AiResponseCacheTtlMinutes = options.ResponseCacheTtlMinutes;
        settings.AiDisclosureAcknowledged = options.DisclosureAcknowledged;
        settings.AiEnabledFeatures = string.Join(',', options.EnabledFeatures);
        settings.AiPerFeatureDailyTokenCeilings = SerializePerFeatureCeilings(options.PerFeatureDailyTokenCeilings);
    }

    private static IReadOnlyCollection<string> ParseEnabledFeatures(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, int> ParsePerFeatureCeilings(string? raw)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var segment in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split('=', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && int.TryParse(parts[1], out var ceiling) && ceiling > 0)
                result[parts[0]] = ceiling;
        }

        return result;
    }

    private static string SerializePerFeatureCeilings(Dictionary<string, int> ceilings)
    {
        if (ceilings.Count == 0)
            return string.Empty;

        return string.Join(';', ceilings.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
