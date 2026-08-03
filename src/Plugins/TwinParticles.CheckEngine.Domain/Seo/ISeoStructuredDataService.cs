namespace TwinParticles.CheckEngine.Domain.Seo;

public interface ISeoStructuredDataService
{
    string BuildVehicleLandingJsonLd(int vehicleConfigurationId, string locale);

    string BuildPartForVehicleJsonLd(int productId, int vehicleConfigurationId, string locale);
}
