using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.GMaster.Services;

namespace Nop.Plugin.Misc.GMaster.Infrastructure;

public sealed class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<GMasterCatalogParser>();
        services.AddSingleton<GMasterImageFactory>();
        services.AddScoped<GMasterCatalogImportService>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 10;
}
