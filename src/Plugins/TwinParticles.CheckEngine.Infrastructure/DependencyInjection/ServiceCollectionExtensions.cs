using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Fitment;
using TwinParticles.CheckEngine.Infrastructure.Garage;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;
using TwinParticles.CheckEngine.Infrastructure.Oem;
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
        services.AddSingleton<IOemNormalizationService, DefaultOemNormalizationService>();
        services.AddScoped<IOemAdminRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemRelationReadRepository, SqlOemAdminRepository>();
        services.AddScoped<IOemSearchReadRepository, SqlOemAdminRepository>();

        services.AddSingleton<IImportExtractionParser, CsvImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser, ExcelImportExtractionParser>();
        services.AddSingleton<IImportExtractionParser, PdfImportExtractionParser>();

        services.AddSingleton<IFitmentCache, MemoryFitmentCache>();
        services.AddSingleton<InMemoryFitmentClaimRepository>();
        services.AddSingleton<IFitmentClaimReadRepository>(sp => sp.GetRequiredService<InMemoryFitmentClaimRepository>());
        services.AddSingleton<IFitmentClaimWriteRepository>(sp => sp.GetRequiredService<InMemoryFitmentClaimRepository>());
        services.AddSingleton<IFitmentReviewQueueRepository, InMemoryFitmentReviewQueueRepository>();

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
        services.AddSingleton<IProductSearchReadRepository, InMemoryProductSearchReadRepository>();
        services.AddSingleton<ISearchIndexHealthService, InMemorySearchIndexHealthService>();

        services.AddSingleton<IGarageRepository, InMemoryGarageRepository>();
        services.AddSingleton<IGarageGuestStore, InMemoryGarageGuestStore>();
        services.AddSingleton<IGarageAuditService, InMemoryGarageAuditService>();

        return services;
    }
}
