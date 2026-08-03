using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class DefaultSeoUrlService : ISeoUrlService
{
    public string BuildVehicleLandingPath(int vehicleConfigurationId, string locale)
    {
        var prefix = locale == "ar" ? "/ar" : string.Empty;
        return $"{prefix}/vehicles/config-{vehicleConfigurationId}";
    }

    public string BuildPartForVehicleLandingPath(int productId, int vehicleConfigurationId, string locale)
    {
        var prefix = locale == "ar" ? "/ar" : string.Empty;
        return $"{prefix}/parts/product-{productId}/for/config-{vehicleConfigurationId}";
    }
}
