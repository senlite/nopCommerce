namespace TwinParticles.CheckEngine.Domain.Vehicle;

public interface IManufacturerVinDecoder
{
    bool CanDecode(string wmi);

    VinDecodeContribution Decode(Vin vin);
}
