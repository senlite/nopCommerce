using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.DependencyInjection;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Infrastructure.Ai;
using TwinParticles.CheckEngine.Infrastructure.DependencyInjection;
using TwinParticles.CheckEngine.Infrastructure.Filters;

namespace TwinParticles.CheckEngine.Infrastructure;

public sealed class CheckEngineStartup : INopStartup
{
    public int Order => 3500;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CheckEngineSettings>(configuration.GetSection(CheckEngineSettings.SectionName));

        services.AddCheckEngineApplication();
        services.AddCheckEngineInfrastructure();

        services.AddSingleton<AiSpendAlertService>();
        services.AddSingleton<AiSpendGuardService>(sp => new AiSpendGuardService(
            sp.GetService<IAiFeatureToggle>(),
            sp.GetService<IAiSpendPolicy>(),
            sp.GetService<IAiUsageLedger>(),
            sp.GetRequiredService<AiSpendAlertService>()));

        services.AddSingleton<AiCompletionGateService>(sp =>
            new AiCompletionGateService(
                sp.GetRequiredService<CachingAiCompletionPort>(),
                sp.GetRequiredService<AiSpendGuardService>(),
                sp.GetService<IAiUsageLedger>(),
                sp.GetService<IAiSpendPolicy>()));
        services.AddSingleton<IAiCompletionPort>(sp => sp.GetRequiredService<AiCompletionGateService>());

        services.AddSingleton<AiEmbeddingGateService>(sp =>
            new AiEmbeddingGateService(
                sp.GetRequiredService<CachingAiEmbeddingPort>(),
                sp.GetRequiredService<AiSpendGuardService>(),
                sp.GetService<IAiUsageLedger>(),
                sp.GetService<IAiSpendPolicy>()));
        services.AddSingleton<IAiEmbeddingPort>(sp => sp.GetRequiredService<AiEmbeddingGateService>());

        services.AddScoped<CheckEngineLicenceWriteFilter>();
        services.AddMvc(options => options.Filters.AddService<CheckEngineLicenceWriteFilter>());

        services.AddScoped<TwinParticles.CheckEngine.Domain.L10n.IAutomotiveGlossaryOverridesSource,
            TwinParticles.CheckEngine.L10n.PluginAutomotiveGlossaryOverridesSource>();
        services.AddScoped<TwinParticles.CheckEngine.Domain.Ai.IAllowedSpecificationKeyOverridesSource,
            TwinParticles.CheckEngine.Ai.PluginAllowedSpecificationKeyOverridesSource>();
        services.AddScoped<TwinParticles.CheckEngine.Domain.Search.ISearchSynonymOverridesSource,
            TwinParticles.CheckEngine.Search.PluginSearchSynonymOverridesSource>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }
}
