using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Application.Workshop;
using TwinParticles.CheckEngine.Application.Fleet;
using TwinParticles.CheckEngine.Application.Dealer;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Observability;
using TwinParticles.CheckEngine.Application.ReferenceScale;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Seo;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckEngineApplication(this IServiceCollection services)
    {
        services.AddScoped<VehicleAliasApplicationService>();
        services.AddScoped<VehicleAliasImportService>();
        services.AddScoped<VehicleAdminService>();
        services.AddScoped<VinDecodeApplicationService>();
        services.AddScoped<OemSupersessionService>();
        services.AddScoped<OemAdminService>();
        services.AddScoped<OemResolveService>();
        services.AddScoped<ImportExtractionService>();
        services.AddScoped<ImportNormalizationService>();
        services.AddScoped<ImportDuplicateDetectionService>();
        services.AddScoped<ImportOemMatchingService>();
        services.AddScoped<ImportVehicleMatchingService>();
        services.AddScoped<ImportAiEnrichmentHookService>();
        services.AddScoped<ImportTranslationHookService>();
        services.AddScoped<ImportSeoGenerationHookService>();
        services.AddScoped<ImportCategorizationService>();
        services.AddScoped<ImportImageAssignmentService>();
        services.AddScoped<ImportReviewService>();
        services.AddScoped<ImportPublicationService>();
        // Scoped because SQL is the authoritative pipeline state. A singleton would capture scoped
        // repositories and reintroduce process-local mutable batch ownership.
        services.AddScoped<ImportPipelineOrchestratorService>();

        services.AddScoped<FitmentEvaluationService>();
        services.AddScoped<FitmentPublicationPolicyService>();
        services.AddScoped<FitmentReviewService>();

        services.AddScoped<UnifiedSearchService>();
        services.AddScoped<SearchIndexAdminService>();
        services.AddScoped<GarageContextSearchService>();
        services.AddScoped<RecommendationService>();
        services.AddScoped<SearchAutocompleteService>();
        services.AddScoped<SearchAnalyticsAdminService>();

        services.AddScoped<GarageService>();
        services.AddScoped<GaragePrivacyService>();
        services.AddScoped<TwinParticles.CheckEngine.Application.Privacy.CheckEngineSubjectDataService>();

        services.AddScoped<ProductImageService>();
        services.AddScoped<BatchImageReplacementService>();
        services.AddScoped<SupplierImageSourcingService>();
        services.AddScoped<ImageImportOrchestrationService>();

        services.AddScoped<LocaleFormattingService>();

        services.AddScoped<SeoLandingService>();
        services.AddScoped<TwinParticles.CheckEngine.Domain.Seo.ISeoLandingRegenerationTrigger, SeoLandingRegenerationTrigger>();

        services.AddScoped<AutomotiveGlossaryService>();
        services.AddScoped<IAllowedSpecificationKeyCatalog, AllowedSpecificationKeyCatalog>();
        services.AddScoped<AiContentCandidateService>();
        services.AddScoped<AiPromptResolver>();
        services.AddScoped<SearchIntentVehicleResolver>();
        services.AddScoped<NaturalLanguageIntentParser>();
        services.AddScoped<AiImportCandidateRebindService>();
        services.AddScoped<FitmentInferenceService>();
        services.AddScoped<BilingualSearchSynonymService>();
        services.AddScoped<SemanticSearchService>();
        services.AddScoped<SearchEmbeddingIndexBuilderService>();
        services.AddScoped<AiDisclosureService>();
        services.AddScoped<CustomerAssistantService>();

        services.AddScoped<AiProposalService>();

        services.AddScoped<ILicenceService, DefaultLicenceService>();
        services.AddSingleton<IMarketplaceOnboardingPolicy>(_ => MarketplaceOnboardingOptions.Current);
        services.AddScoped<MarketplaceLicenceGate>();
        services.AddScoped<WorkshopPortalLicenceGate>();
        services.AddScoped<FleetPortalLicenceGate>();
        services.AddScoped<DealerPortalLicenceGate>();
        services.AddScoped<VendorOnboardingService>();
        services.AddScoped<MarketplaceUpgradeService>();
        services.AddScoped<VendorIsolationService>();
        services.AddScoped<VendorDashboardService>();
        services.AddScoped<VendorFitmentContributionService>();
        services.AddScoped<CommissionEvaluationService>();
        services.AddScoped<CommissionPlanAdminService>();
        services.AddScoped<CommissionSnapshotService>();
        services.AddScoped<PayoutStatementBuilder>();
        services.AddScoped<PayoutStatementService>();
        services.AddScoped<OrderVendorSplitService>();
        services.AddScoped<WorkshopJobService>();
        services.AddScoped<FleetPortalService>();
        services.AddScoped<DealerPortalService>();
        services.AddScoped<ErpSyncService>();
        services.AddScoped<ErpInboundWebhookService>();
        services.AddScoped<CheckEngineHealthService>();
        services.AddScoped<CheckEngineLicenceGate>();
        services.AddScoped<CheckEngineUninstallExportService>();
        services.AddScoped<ReferenceScaleCatalogService>();

        return services;
    }
}
