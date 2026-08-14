namespace TwinParticles.CheckEngine.Domain.Oem;

/// <summary>
/// Outcome of a bulk OEM number upsert keyed on (ManufacturerId, NormalizedNumber) per FR-236.
/// </summary>
public sealed class OemBulkUpsertResult
{
    public int Inserted { get; init; }

    public int Updated { get; init; }

    public int TotalProcessed => Inserted + Updated;

    public static OemBulkUpsertResult Empty { get; } = new();
}
