using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Garage;

public sealed class Garage
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? ActiveGarageVehicleId { get; set; }

    public List<GarageVehicle> Vehicles { get; set; } = [];

    public List<GarageOem> Oems { get; set; } = [];

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
