namespace TwinParticles.CheckEngine.Models;

public sealed class SeoVehicleLandingRequestModel
{
    public int VehicleConfigurationId { get; set; }

    public string Locale { get; set; } = "en";
}

public sealed class SeoPartForVehicleLandingRequestModel
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public string Locale { get; set; } = "en";
}
