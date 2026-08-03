using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

public sealed class VinDecoderRegistry : IVinDecoderRegistry
{
    private readonly IReadOnlyList<IManufacturerVinDecoder> _decoders;

    public VinDecoderRegistry(IEnumerable<IManufacturerVinDecoder> decoders)
    {
        _decoders = [.. decoders];
    }

    public IManufacturerVinDecoder? Resolve(string wmi)
    {
        foreach (var decoder in _decoders)
        {
            if (decoder.CanDecode(wmi))
            {
                return decoder;
            }
        }

        return null;
    }
}
