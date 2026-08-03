namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public interface IVehicleAliasNormalizationService
{
    string Normalize(string text, string locale);
}
