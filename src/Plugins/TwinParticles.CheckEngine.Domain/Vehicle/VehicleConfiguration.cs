namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VehicleConfiguration
{
    public int Id { get; set; }

    public int GenerationId { get; set; }

    public int? BodyId { get; set; }

    public int? EngineId { get; set; }

    public int? MarketId { get; set; }

    public string TrimName { get; set; } = string.Empty;

    public int? ProductionFromYear { get; set; }

    public int? ProductionToYear { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
