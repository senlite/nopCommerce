using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using TwinParticles.CheckEngine.Application.DependencyInjection;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Infrastructure.DependencyInjection;

namespace TwinParticles.CheckEngine.Infrastructure;

public sealed class CheckEngineStartup : INopStartup
{
    public int Order => 3500;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CheckEngineSettings>(configuration.GetSection(CheckEngineSettings.SectionName));

        services.AddCheckEngineApplication();
        services.AddCheckEngineInfrastructure();
    }

    public void Configure(IApplicationBuilder application)
    {
    }
}
