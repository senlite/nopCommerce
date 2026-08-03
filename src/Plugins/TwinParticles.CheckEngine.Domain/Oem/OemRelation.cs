using System;

namespace TwinParticles.CheckEngine.Domain.Oem;

public sealed class OemRelation
{
    public int Id { get; set; }

    public int FromOemNumberId { get; set; }

    public int ToOemNumberId { get; set; }

    public OemRelationType RelationType { get; set; }

    public DateTime? ValidFromUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
