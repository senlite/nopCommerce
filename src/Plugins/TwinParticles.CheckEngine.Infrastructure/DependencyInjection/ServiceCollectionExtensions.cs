using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Images;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.L10n;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.ReferenceScale;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Ai;
using TwinParticles.CheckEngine.Infrastructure.Erp;
using TwinParticles.CheckEngine.Infrastructure.Fitment;
using TwinParticles.CheckEngine.Infrastructure.Garage;
using TwinParticles.CheckEngine.Infrastructure.Images;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;
using TwinParticles.CheckEngine.Infrastructure.L10n;
using TwinParticles.CheckEngine.Infrastructure.Licensing;
using TwinParticles.CheckEngine.Infrastructure.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Oem;
using TwinParticles.CheckEngine.Infrastructure.Seo;
using TwinParticles.CheckEngine.Infrastructure.Observability;
using TwinParticles.CheckEngine.Infrastructure.Performance;
using TwinParticles.CheckEngine.Infrastructure.ReferenceScale;
using TwinParticles.CheckEngine.Infrastructure.Search;
using TwinParticles.CheckEngine.Infrastructure.Security;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckEngineInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICheckEngineTelemetry, LoggerCheckEngineTelemetry>();
        services.AddSingleton<ICheckEngineClock, SystemCheckEngineClock>();
        services.AddSingleton<ICheckEngineInputSanitizer, DefaultCheckEngineInputSanitizer>();
        services.AddSingleton<InMemoryCheckEngineAuditService>();
        // Prefer SQL audit when INopDataProvider is available; InMemory remains registered above for tests/local.
        services.AddScoped<SqlCheckEngineAuditService>();
        services.AddScoped<ICheckEngineAuditService>(sp => sp.GetRequiredService<SqlCheckEngineAuditService>());
        services.AddScoped<IAuditIntegrityService>(sp => sp.GetRequiredService<SqlCheckEngineAuditService>());
        services.AddSingleton<IOemNormalizationService, DefaultOemNormalizationService>();
        services.AddScoped<IOemAdminRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemRelationReadRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemSearchReadRepository, SqlOemAdminRepository>();

        services.AddSingleton<IImportExtractionParser, CsvImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser, ExcelImportExtractionParser>();
        services.AddSingleton<IImportPdfOcrPort>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CheckEngineSettings>>().Value;
            var envCommand = Environment.GetEnvironmentVariable("CHECKENGINE_OCR_COMMAND");
            var command = !string.IsNullOrWhiteSpace(envCommand) ? envCommand : settings.Import.OcrCommand;
            var enabled = settings.Import.OcrEnabled || !string.IsNullOrWhiteSpace(envCommand);
            if (enabled && !string.IsNullOrWhiteSpace(command))
                return new ExternalProcessImportPdfOcrPort(command);

            return NullImportPdfOcrPort.Instance;
        });
        services.AddSingleton<PdfImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser>(sp => sp.GetRequiredService<PdfImportExtractionParser>());

        services.AddSingleton<IFitmentCache, MemoryFitmentCache>();
        services.AddScoped<SqlFitmentClaimRepository>();
        services.AddScoped<IFitmentClaimReadRepository>(sp => sp.GetRequiredService<SqlFitmentClaimRepository>());
        services.AddScoped<IFitmentClaimWriteRepository>(sp => sp.GetRequiredService<SqlFitmentClaimRepository>());
        services.AddScoped<IFitmentReviewQueueRepository, SqlFitmentReviewQueueRepository>();
        services.AddScoped<IImportPipelineRepository, SqlImportPipelineRepository>();
        services.AddScoped<IImportProductPublisher, NopImportProductPublisher>();
        services.AddScoped<IProductOemMapRepository, SqlProductOemMapRepository>();

        services.AddSingleton<IVehicleAliasNormalizationService, VehicleAliasNormalizationService>();
        services.AddSingleton<IVehicleAliasCache, MemoryVehicleAliasCache>();
        services.AddScoped<IVehicleAliasSqlExecutor, NopDataProviderVehicleAliasSqlExecutor>();
        services.AddScoped<IVehicleAliasReadRepository, SqlVehicleAliasRepository>();
        services.AddScoped<IVehicleAliasWriteRepository, SqlVehicleAliasRepository>();

        services.AddScoped<IVehicleAdminRepository, SqlVehicleAdminRepository>();
        services.AddScoped<IVinSupportRepository, SqlVinSupportRepository>();
        services.AddScoped<BmwVinConfigurationResolver>();
        services.AddScoped<BmwVinPatternSeedLoader>();
        services.AddSingleton<IVinPrivacyService, VinPrivacyService>();
        services.AddSingleton(_ => VinDecodeOptions.Current);
        services.AddSingleton(provider =>
        {
            var catalog = BmwVinPatternCatalog.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            return new BmwVinWmiAllowList(catalog.Wmis.Select(wmi => wmi.Wmi));
        });
        services.AddScoped<IVehicleSeedLoader, BmwReferenceVehicleSeedLoader>();

        services.AddScoped<IManufacturerVinDecoder, BmwVinDecoder>();
        services.AddScoped<IVinDecoderRegistry, VinDecoderRegistry>();
        services.AddSingleton<IVinDecodeRateLimiter, InMemoryVinDecodeRateLimiter>();

        services.AddSingleton<IBilingualSearchTextNormalizer, DefaultBilingualSearchTextNormalizer>();
        services.AddSingleton<ISearchQueryFingerprintService, HmacSearchQueryFingerprintService>();
        services.AddScoped<IProductSearchReadRepository, SqlProductSearchReadRepository>();
        services.AddScoped<ISearchAnalyticsService, SqlSearchAnalyticsService>();
        // Durable, web-farm-shared search index state + incremental catalog projection.
        services.AddScoped<SqlSearchIndexHealthService>();
        services.AddScoped<ISearchIndexHealthService>(sp => sp.GetRequiredService<SqlSearchIndexHealthService>());
        services.AddScoped<ISearchIndexStateReader>(sp => sp.GetRequiredService<SqlSearchIndexHealthService>());
        services.AddSingleton<ISearchRateLimiter, InMemorySearchRateLimiter>();

        services.AddScoped<IGarageVinProtector, NopGarageVinProtector>();
        services.AddScoped<IGarageRepository, SqlGarageRepository>();
        services.AddSingleton<IGarageGuestStore, InMemoryGarageGuestStore>();
        services.AddSingleton<IGarageAuditService, InMemoryGarageAuditService>();

        services.AddScoped<IProductImageRepository, SqlProductImageRepository>();
        services.AddScoped<IImageStorageService, NopPictureImageStorageService>();
        services.AddScoped<IImageDeliveryService, ConfigurableCdnImageDeliveryService>();
        services.AddScoped<IImageQuarantineService, SafeImageQuarantineService>();
        services.AddScoped<IProductLookupService, NopProductLookupService>();

        services.AddScoped<IReferenceScaleCatalogLoader, ReferenceScaleCatalogLoader>();

        services.AddSingleton<ILocaleFormattingService, DefaultLocaleFormattingService>();

        services.AddSingleton<InMemoryLicenceStateStore>();
        services.AddScoped<SqlLicenceStateStore>();
        services.AddScoped<ILicenceStateStore>(sp => sp.GetRequiredService<SqlLicenceStateStore>());
        services.AddSingleton<ILicenceKeyValidator, HmacLicenceKeyValidator>();
        services.AddScoped<IVendorSecretProtector, NopVendorSecretProtector>();
        services.AddScoped<IVendorRepository, SqlVendorRepository>();
        services.AddScoped<IVendorOwnershipStore, SqlVendorOwnershipStore>();
        services.AddScoped<IVendorCommerceCatalog, SqlVendorCommerceCatalog>();
        services.AddScoped<IVendorOrderReadStore, SqlVendorOrderReadStore>();
        services.AddScoped<IVendorInventoryStore, SqlVendorInventoryStore>();
        services.AddScoped<IVendorAnalyticsStore, SqlVendorAnalyticsStore>();
        services.AddScoped<ICommissionPlanRepository, SqlCommissionPlanRepository>();
        services.AddScoped<ICommissionSnapshotStore, SqlCommissionSnapshotStore>();
        services.AddScoped<IVendorProductCategoryStore, SqlVendorProductCategoryStore>();

        services.AddScoped<ISeoLandingRepository, SqlSeoLandingRepository>();
        services.AddScoped<ISeoIndexabilityPolicy, SqlSeoIndexabilityPolicy>();
        services.AddSingleton<ISeoUrlService, DefaultSeoUrlService>();
        services.AddSingleton<ISeoStructuredDataService, DefaultSeoStructuredDataService>();
        services.AddScoped<ISeoSitemapService, SqlBackedSeoSitemapService>();
        services.AddSingleton<ISeoPerformanceBudgetService, DefaultSeoPerformanceBudgetService>();

        services.AddScoped<IErpSyncQueueRepository, SqlErpSyncQueueRepository>();
        services.AddSingleton<IErpClientAdapter, ErpNextHttpClientAdapter>();
        services.AddSingleton<IErpConflictResolutionService, DefaultErpConflictResolutionService>();
        services.AddSingleton<IErpInboundWebhookValidator, HmacErpInboundWebhookValidator>();
        services.AddScoped<IErpReconciliationDataSource, NopErpReconciliationDataSource>();

        services.AddSingleton(_ => CheckEngineAiOptions.Current);
        services.AddSingleton<OpenAiCompatibleCompletionPort>();
        services.AddSingleton<AzureOpenAiCompletionPort>();
        services.AddSingleton<AnthropicCompletionPort>();
        services.AddSingleton<AiCompletionProviderRouter>();
        services.AddSingleton<MemoryAiCompletionCache>();
        services.AddSingleton<IAiCompletionCache>(sp =>
        {
            var memory = sp.GetRequiredService<MemoryAiCompletionCache>();
            var staticCache = sp.GetService<Nop.Core.Caching.IStaticCacheManager>();
            if (staticCache is null)
                return memory;

            var distributed = new NopStaticCacheAiCompletionCache(staticCache, sp.GetRequiredService<CheckEngineAiOptions>());
            return new LayeredAiCompletionCache(memory, distributed);
        });
        services.AddSingleton<CachingAiCompletionPort>(sp => new CachingAiCompletionPort(
            sp.GetRequiredService<AiCompletionProviderRouter>(),
            sp.GetRequiredService<IAiCompletionCache>(),
            sp.GetRequiredService<CheckEngineAiOptions>()));
        services.AddSingleton<MemoryAiEmbeddingCache>();
        services.AddSingleton<IAiEmbeddingCache>(sp =>
        {
            var memory = sp.GetRequiredService<MemoryAiEmbeddingCache>();
            var staticCache = sp.GetService<Nop.Core.Caching.IStaticCacheManager>();
            if (staticCache is null)
                return memory;

            var distributed = new NopStaticCacheAiEmbeddingCache(staticCache, sp.GetRequiredService<CheckEngineAiOptions>());
            return new LayeredAiEmbeddingCache(memory, distributed);
        });
        services.AddSingleton<CachingAiEmbeddingPort>(sp => new CachingAiEmbeddingPort(
            sp.GetRequiredService<AiEmbeddingProviderRouter>(),
            sp.GetRequiredService<IAiEmbeddingCache>(),
            sp.GetRequiredService<CheckEngineAiOptions>()));
        services.AddSingleton<IAiSpendPolicy, SettingsAiSpendPolicy>();
        services.AddSingleton<IAiFeatureToggle, SettingsAiFeatureToggle>();
        services.AddSingleton<OpenAiCompatibleEmbeddingPort>();
        services.AddSingleton<AzureOpenAiEmbeddingPort>();
        services.AddSingleton<DeterministicTextEmbeddingPort>();
        services.AddSingleton<AiEmbeddingProviderRouter>();
        services.AddSingleton<IAiDisclosureAcknowledgement, SettingsAiDisclosureAcknowledgement>();
        services.AddSingleton<IAiPromptStore, EmbeddedAiPromptStore>();
        services.AddScoped<ISearchEmbeddingIndex, SqlSearchEmbeddingIndex>();
        services.AddScoped<ISearchEmbeddingCatalogSource, SqlSearchEmbeddingCatalogSource>();
        services.AddSingleton<IAiUsageLedger, SqlAiUsageLedger>();
        services.AddSingleton<IAiDataDisclosureCatalog, DefaultAiDataDisclosureCatalog>();
        services.AddScoped<IAiGenerationRepository, SqlAiGenerationRepository>();
        services.AddScoped<IAiContentApplicator, NopAiContentApplicator>();
        services.AddScoped<ICheckEngineDatabaseHealthProbe, NopDataProviderDatabaseHealthProbe>();

        return services;
    }
}
