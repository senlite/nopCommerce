namespace TwinParticles.CheckEngine.Domain.Seo;

public interface ISeoUrlService
{
    string BuildVehicleLandingPath(int vehicleConfigurationId, string locale);

    string BuildPartForVehicleLandingPath(int productId, int vehicleConfigurationId, string locale);
}
