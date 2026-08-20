using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Configuration;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.DependencyInjection;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Configuration;
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

    /// <summary>
    /// Hydrates <see cref="CheckEngineAiOptions.Current"/> from persisted settings on every start.
    ///
    /// The options object is a process-wide singleton, so without this an application restart — or any
    /// additional node in a web farm — serves with no enabled AI features, no provider credentials and
    /// a reset disclosure acknowledgement until an administrator happens to open the configure page.
    /// </summary>
    public void Configure(IApplicationBuilder application)
    {
        application.UseMiddleware<TenantResolutionMiddleware>();

        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        try
        {
            using var scope = application.ApplicationServices.CreateScope();
            var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
            var settings = settingService.LoadSettingAsync<CheckEnginePluginSettings>().GetAwaiter().GetResult();
            CheckEngineAiSettingsSync.Apply(settings);
        }
        catch (Exception exception)
        {
            // A store that cannot read plugin settings must still boot; AI features stay off until
            // the settings load successfully, which matches the disabled-by-default posture.
            application.ApplicationServices
                .GetService<ILogger<CheckEngineStartup>>()?
                .LogWarning(exception, "Check Engine could not hydrate AI options at startup; AI features remain disabled.");
        }
    }
}
