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

        services.AddSingleton<AiCompletionGateService>(sp =>
            new AiCompletionGateService(
                sp.GetRequiredService<CachingAiCompletionPort>(),
                sp.GetService<IAiFeatureToggle>(),
                sp.GetService<IAiUsageLedger>(),
                sp.GetService<IAiSpendPolicy>()));
        services.AddSingleton<IAiCompletionPort>(sp => sp.GetRequiredService<AiCompletionGateService>());

        services.AddScoped<CheckEngineLicenceWriteFilter>();
        services.AddMvc(options => options.Filters.AddService<CheckEngineLicenceWriteFilter>());
    }

    public void Configure(IApplicationBuilder application)
    {
    }
}
