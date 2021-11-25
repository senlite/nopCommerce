using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Pickup.PickupInStore.Factories;
using Nop.Plugin.Pickup.PickupInStore.Services;

namespace Nop.Plugin.Pickup.PickupInStore.Infrastructure
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
            services.AddScoped<IStorePickupPointService, StorePickupPointService>();
            services.AddScoped<IStorePickupPointModelFactory, StorePickupPointModelFactory>();
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