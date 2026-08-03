using System;

namespace TwinParticles.CheckEngine.Domain.Garage;

public sealed class GarageOem
{
    public int Id { get; set; }

    public int GarageId { get; set; }

    public int OemNumberId { get; set; }

    public string? DisplayNumber { get; set; }

    public DateTime CreatedUtc { get; set; }
}
