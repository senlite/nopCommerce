using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Shipping.Bosta.Services;

namespace Nop.Plugin.Shipping.Bosta.Infrastructure;

public class BostaStartup : INopStartup
{
    public int Order => 3600;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BostaRateCalculator>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }
}
