using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Services;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITaxProvider, FixedOrByCountryStateZipTaxProvider>();
            services.AddScoped<ICountryStateZipService, CountryStateZipService>();
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order => 3000;
    }
}