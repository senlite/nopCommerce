using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Ai;
using TwinParticles.CheckEngine.Models;

namespace TwinParticles.CheckEngine.Configuration;

public static class CheckEngineConfigurationMapper
{
    public static ConfigurationModel ToModel(CheckEnginePluginSettings settings)
    {
        CheckEngineAiSettingsSync.Apply(settings);

        var enabled = new HashSet<string>(CheckEngineAiOptions.Current.EnabledFeatures, System.StringComparer.OrdinalIgnoreCase);
        return new ConfigurationModel
        {
            Enabled = settings.Enabled,
            AiProviderKind = settings.AiProviderKind,
            AiEmbeddingProviderKind = settings.AiEmbeddingProviderKind,
            AiBaseUrl = settings.AiBaseUrl,
            AiApiKey = settings.AiApiKey,
            AiModel = settings.AiModel,
            AiEmbeddingModel = settings.AiEmbeddingModel,
            AiEmbeddingBaseUrl = settings.AiEmbeddingBaseUrl,
            AiEmbeddingApiKey = settings.AiEmbeddingApiKey,
            AiAzureDeploymentName = settings.AiAzureDeploymentName,
            AiAzureEmbeddingDeploymentName = settings.AiAzureEmbeddingDeploymentName,
            AiDailyTokenCeiling = settings.AiDailyTokenCeiling,
            AiTokenCostPer1KUsd = settings.AiTokenCostPer1KUsd,
            AiResponseCacheTtlMinutes = settings.AiResponseCacheTtlMinutes,
            AiDisclosureAcknowledged = settings.AiDisclosureAcknowledged,
            AiEnabledFeatures = settings.AiEnabledFeatures,
            AiPerFeatureDailyTokenCeilings = settings.AiPerFeatureDailyTokenCeilings,
            EnableImportEnrichment = enabled.Contains(AiFeatureKeys.ImportEnrichment),
            EnableImportTranslation = enabled.Contains(AiFeatureKeys.ImportTranslation),
            EnableImportSeo = enabled.Contains(AiFeatureKeys.ImportSeo),
            EnableSearchNaturalLanguage = enabled.Contains(AiFeatureKeys.SearchNaturalLanguage),
            EnableSearchSemantic = enabled.Contains(AiFeatureKeys.SearchSemantic),
            EnableFitmentInference = enabled.Contains(AiFeatureKeys.FitmentInference),
            EnableCustomerAssistant = enabled.Contains(AiFeatureKeys.CustomerAssistant),
            EnableRecommendations = enabled.Contains(AiFeatureKeys.Recommendations),
            EnableMarketplace = settings.EnableMarketplace,
            MarketplaceAgreementVersion = string.IsNullOrWhiteSpace(settings.MarketplaceAgreementVersion)
                ? VendorAgreementVersions.Default
                : settings.MarketplaceAgreementVersion
        };
    }

    public static void ApplyModel(CheckEnginePluginSettings settings, ConfigurationModel model)
    {
        settings.Enabled = model.Enabled;
        settings.AiProviderKind = model.AiProviderKind;
        settings.AiEmbeddingProviderKind = model.AiEmbeddingProviderKind;
        settings.AiBaseUrl = model.AiBaseUrl;
        settings.AiApiKey = model.AiApiKey;
        settings.AiModel = model.AiModel;
        settings.AiEmbeddingModel = model.AiEmbeddingModel;
        settings.AiEmbeddingBaseUrl = model.AiEmbeddingBaseUrl;
        settings.AiEmbeddingApiKey = model.AiEmbeddingApiKey;
        settings.AiAzureDeploymentName = model.AiAzureDeploymentName;
        settings.AiAzureEmbeddingDeploymentName = model.AiAzureEmbeddingDeploymentName;
        settings.AiDailyTokenCeiling = model.AiDailyTokenCeiling;
        settings.AiTokenCostPer1KUsd = model.AiTokenCostPer1KUsd;
        settings.AiResponseCacheTtlMinutes = model.AiResponseCacheTtlMinutes;
        settings.AiDisclosureAcknowledged = model.AiDisclosureAcknowledged;
        settings.AiPerFeatureDailyTokenCeilings = model.AiPerFeatureDailyTokenCeilings;

        var enabledFeatures = new List<string>();
        if (model.EnableImportEnrichment) enabledFeatures.Add(AiFeatureKeys.ImportEnrichment);
        if (model.EnableImportTranslation) enabledFeatures.Add(AiFeatureKeys.ImportTranslation);
        if (model.EnableImportSeo) enabledFeatures.Add(AiFeatureKeys.ImportSeo);
        if (model.EnableSearchNaturalLanguage) enabledFeatures.Add(AiFeatureKeys.SearchNaturalLanguage);
        if (model.EnableSearchSemantic) enabledFeatures.Add(AiFeatureKeys.SearchSemantic);
        if (model.EnableFitmentInference) enabledFeatures.Add(AiFeatureKeys.FitmentInference);
        if (model.EnableCustomerAssistant) enabledFeatures.Add(AiFeatureKeys.CustomerAssistant);
        if (model.EnableRecommendations) enabledFeatures.Add(AiFeatureKeys.Recommendations);

        settings.AiEnabledFeatures = string.Join(',', enabledFeatures);
        settings.EnableMarketplace = model.EnableMarketplace;
        settings.MarketplaceAgreementVersion = string.IsNullOrWhiteSpace(model.MarketplaceAgreementVersion)
            ? VendorAgreementVersions.Default
            : model.MarketplaceAgreementVersion.Trim();
        CheckEngineAiSettingsSync.Apply(settings);
        MarketplaceOnboardingOptions.Current.ApplicationsOpen = settings.EnableMarketplace;
        MarketplaceOnboardingOptions.Current.CurrentAgreementVersion = settings.MarketplaceAgreementVersion;
    }

    public static bool HasAnyAiFeatureEnabled(ConfigurationModel model) =>
        model.EnableImportEnrichment
        || model.EnableImportTranslation
        || model.EnableImportSeo
        || model.EnableSearchNaturalLanguage
        || model.EnableSearchSemantic
        || model.EnableFitmentInference
        || model.EnableCustomerAssistant
        || model.EnableRecommendations;
}
