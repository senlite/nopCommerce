using Nop.Core.Configuration;

namespace TwinParticles.CheckEngine.Configuration;

public sealed class CheckEnginePluginSettings : ISettings
{
    public bool Enabled { get; set; } = true;

    public DateTime? UninstallExportPreparedUtc { get; set; }

    public int AiProviderKind { get; set; } = (int)Domain.Ai.AiProviderKind.OpenAiCompatible;

    public string? AiBaseUrl { get; set; }

    public string? AiApiKey { get; set; }

    public string? AiModel { get; set; } = "gpt-4o-mini";

    public string? AiEmbeddingModel { get; set; } = "text-embedding-3-small";

    public string? AiAzureDeploymentName { get; set; }

    public string? AiAzureEmbeddingDeploymentName { get; set; }

    public int AiDailyTokenCeiling { get; set; } = 100_000;

    public int AiResponseCacheTtlMinutes { get; set; } = 60;

    public bool AiDisclosureAcknowledged { get; set; }

    /// <summary>Comma-separated AiFeatureKeys values.</summary>
    public string? AiEnabledFeatures { get; set; }

    /// <summary>Semicolon-separated feature=ceiling pairs.</summary>
    public string? AiPerFeatureDailyTokenCeilings { get; set; }
}
