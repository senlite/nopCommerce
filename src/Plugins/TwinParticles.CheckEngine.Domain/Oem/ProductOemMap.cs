using System;

namespace TwinParticles.CheckEngine.Domain.Oem;

public sealed class ProductOemMap
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int OemNumberId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedUtc { get; set; }
}
