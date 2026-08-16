using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace TwinParticles.CheckEngine.Models;

public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiProviderKind")]
    public int AiProviderKind { get; set; } = 1;

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingProviderKind")]
    public int AiEmbeddingProviderKind { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiBaseUrl")]
    public string? AiBaseUrl { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiApiKey")]
    [DataType(DataType.Password)]
    public string? AiApiKey { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiModel")]
    public string? AiModel { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingModel")]
    public string? AiEmbeddingModel { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingBaseUrl")]
    public string? AiEmbeddingBaseUrl { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEmbeddingApiKey")]
    [DataType(DataType.Password)]
    public string? AiEmbeddingApiKey { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureDeploymentName")]
    public string? AiAzureDeploymentName { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiAzureEmbeddingDeploymentName")]
    public string? AiAzureEmbeddingDeploymentName { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDailyTokenCeiling")]
    public int AiDailyTokenCeiling { get; set; } = 100_000;

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiTokenCostPer1KUsd")]
    public decimal AiTokenCostPer1KUsd { get; set; } = 0.002m;

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiResponseCacheTtlMinutes")]
    public int AiResponseCacheTtlMinutes { get; set; } = 60;

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiDisclosureAcknowledged")]
    public bool AiDisclosureAcknowledged { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiEnabledFeatures")]
    public string? AiEnabledFeatures { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.AiPerFeatureDailyTokenCeilings")]
    public string? AiPerFeatureDailyTokenCeilings { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportEnrichment")]
    public bool EnableImportEnrichment { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportTranslation")]
    public bool EnableImportTranslation { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableImportSeo")]
    public bool EnableImportSeo { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchNaturalLanguage")]
    public bool EnableSearchNaturalLanguage { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableSearchSemantic")]
    public bool EnableSearchSemantic { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableFitmentInference")]
    public bool EnableFitmentInference { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableCustomerAssistant")]
    public bool EnableCustomerAssistant { get; set; }

    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.EnableRecommendations")]
    public bool EnableRecommendations { get; set; }
}
