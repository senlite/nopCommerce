using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public interface IManufacturerVinDecoder
{
    bool CanDecode(string wmi);

    VinDecodeContribution Decode(Vin vin);

    Task<VinDecodeContribution> DecodeAsync(Vin vin, CancellationToken cancellationToken)
        => Task.FromResult(Decode(vin));
}
