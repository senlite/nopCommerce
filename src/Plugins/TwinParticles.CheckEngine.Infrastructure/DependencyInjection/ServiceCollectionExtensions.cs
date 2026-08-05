using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Images;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.L10n;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;
using TwinParticles.CheckEngine.Domain.Performance;
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
using TwinParticles.CheckEngine.Infrastructure.Oem;
using TwinParticles.CheckEngine.Infrastructure.Seo;
using TwinParticles.CheckEngine.Infrastructure.Observability;
using TwinParticles.CheckEngine.Infrastructure.Performance;
using TwinParticles.CheckEngine.Infrastructure.Search;
using TwinParticles.CheckEngine.Infrastructure.Security;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;
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
        services.AddScoped<ICheckEngineAuditService, SqlCheckEngineAuditService>();
        services.AddSingleton<IOemNormalizationService, DefaultOemNormalizationService>();
        services.AddScoped<IOemAdminRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemRelationReadRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemSearchReadRepository, SqlOemAdminRepository>();

        services.AddSingleton<IImportExtractionParser, CsvImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser, ExcelImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser, PdfImportExtractionParser>();

        services.AddSingleton<IFitmentCache, MemoryFitmentCache>();
        services.AddScoped<SqlFitmentClaimRepository>();
        services.AddScoped<IFitmentClaimReadRepository>(sp => sp.GetRequiredService<SqlFitmentClaimRepository>());
        services.AddScoped<IFitmentClaimWriteRepository>(sp => sp.GetRequiredService<SqlFitmentClaimRepository>());
        services.AddScoped<IFitmentReviewQueueRepository, SqlFitmentReviewQueueRepository>();
        services.AddScoped<IImportPipelineRepository, SqlImportPipelineRepository>();
        services.AddScoped<IProductOemMapRepository, SqlProductOemMapRepository>();

        services.AddSingleton<IVehicleAliasNormalizationService, VehicleAliasNormalizationService>();
        services.AddSingleton<IVehicleAliasCache, MemoryVehicleAliasCache>();
        services.AddScoped<IVehicleAliasSqlExecutor, NopDataProviderVehicleAliasSqlExecutor>();
        services.AddScoped<IVehicleAliasReadRepository, SqlVehicleAliasRepository>();
        services.AddScoped<IVehicleAliasWriteRepository, SqlVehicleAliasRepository>();

        services.AddScoped<IVehicleAdminRepository, SqlVehicleAdminRepository>();
        services.AddScoped<IVehicleSeedLoader, BasicVehicleSeedLoader>();

        services.AddSingleton<IManufacturerVinDecoder, BmwVinDecoder>();
        services.AddSingleton<IVinDecoderRegistry, VinDecoderRegistry>();
        services.AddSingleton<IVinDecodeRateLimiter, InMemoryVinDecodeRateLimiter>();

        services.AddSingleton<IBilingualSearchTextNormalizer, DefaultBilingualSearchTextNormalizer>();
        services.AddScoped<IProductSearchReadRepository, SqlProductSearchReadRepository>();
        services.AddSingleton<ISearchIndexHealthService, InMemorySearchIndexHealthService>();
        services.AddSingleton<ISearchRateLimiter, InMemorySearchRateLimiter>();

        services.AddScoped<IGarageRepository, SqlGarageRepository>();
        services.AddSingleton<IGarageGuestStore, InMemoryGarageGuestStore>();
        services.AddSingleton<IGarageAuditService, InMemoryGarageAuditService>();

        services.AddScoped<IProductImageRepository, SqlProductImageRepository>();
        services.AddScoped<IImageStorageService, NopPictureImageStorageService>();
        services.AddScoped<IImageDeliveryService, ConfigurableCdnImageDeliveryService>();
        services.AddScoped<IImageQuarantineService, SafeImageQuarantineService>();

        services.AddSingleton<ILocaleFormattingService, DefaultLocaleFormattingService>();

        services.AddSingleton<ILicenceStateStore, InMemoryLicenceStateStore>();

        services.AddScoped<ISeoLandingRepository, SqlSeoLandingRepository>();
        services.AddSingleton<ISeoUrlService, DefaultSeoUrlService>();
        services.AddSingleton<ISeoStructuredDataService, DefaultSeoStructuredDataService>();
        services.AddSingleton<ISeoSitemapService, InMemorySeoSitemapService>();
        services.AddSingleton<ISeoPerformanceBudgetService, DefaultSeoPerformanceBudgetService>();

        services.AddScoped<IErpSyncQueueRepository, SqlErpSyncQueueRepository>();
        services.AddSingleton<IErpClientAdapter, ErpNextHttpClientAdapter>();
        services.AddSingleton<IErpConflictResolutionService, DefaultErpConflictResolutionService>();

        services.AddSingleton(_ => CheckEngineAiOptions.Current);
        services.AddSingleton<IAiCompletionPort, OpenAiCompatibleCompletionPort>();
        services.AddSingleton<IAiUsageLedger, InMemoryAiUsageLedger>();
        services.AddSingleton<IAiFeatureToggle, SettingsAiFeatureToggle>();
        services.AddScoped<ICheckEngineDatabaseHealthProbe, NopDataProviderDatabaseHealthProbe>();

        return services;
    }
}
