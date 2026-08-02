using Microsoft.Extensions.DependencyInjection;

namespace TwinParticles.CheckEngine.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckEngineApplication(this IServiceCollection services)
    {
        return services;
    }
}
