using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class DefaultSeoStructuredDataService : ISeoStructuredDataService
{
    public string BuildVehicleLandingJsonLd(int vehicleConfigurationId, string locale)
    {
        return $"{{\"@context\":\"https://schema.org\",\"@type\":\"ItemList\",\"name\":\"Vehicle {vehicleConfigurationId}\",\"inLanguage\":\"{locale}\"}}";
    }

    public string BuildPartForVehicleJsonLd(int productId, int vehicleConfigurationId, string locale)
    {
        return $"{{\"@context\":\"https://schema.org\",\"@type\":\"Product\",\"sku\":\"{productId}\",\"isRelatedTo\":\"config-{vehicleConfigurationId}\",\"inLanguage\":\"{locale}\"}}";
    }
}
