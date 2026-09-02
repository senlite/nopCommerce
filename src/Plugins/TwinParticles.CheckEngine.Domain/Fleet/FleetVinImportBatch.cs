namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetVinImportBatch
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public int TotalRows { get; set; }

    public int SucceededRows { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public IReadOnlyList<FleetVinImportRow> Rows { get; set; } = Array.Empty<FleetVinImportRow>();
}
