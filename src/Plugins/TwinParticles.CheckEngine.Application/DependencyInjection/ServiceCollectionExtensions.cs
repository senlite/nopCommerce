using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;

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
        services.AddSingleton<ImportPipelineOrchestratorService>();

        services.AddScoped<FitmentEvaluationService>();
        services.AddScoped<FitmentPublicationPolicyService>();
        services.AddScoped<FitmentReviewService>();

        services.AddScoped<UnifiedSearchService>();
        services.AddScoped<SearchIndexAdminService>();
        services.AddScoped<GarageContextSearchService>();

        services.AddScoped<GarageService>();

        return services;
    }
}
