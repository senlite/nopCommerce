namespace TwinParticles.CheckEngine.Domain.Vehicle;

public interface IVinDecoderRegistry
{
    IManufacturerVinDecoder? Resolve(string wmi);
}
