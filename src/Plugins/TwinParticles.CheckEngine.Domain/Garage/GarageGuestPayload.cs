using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Garage;

public sealed class GarageGuestPayload
{
    public List<GarageVehicle> Vehicles { get; set; } = [];

    public List<GarageOem> Oems { get; set; } = [];

    public int? ActiveVehicleId { get; set; }
}
