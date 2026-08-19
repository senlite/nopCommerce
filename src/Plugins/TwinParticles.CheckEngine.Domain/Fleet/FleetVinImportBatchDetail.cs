using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetVinImportBatchDetail
{
    public FleetVinImportBatch Batch { get; set; } = new();

    public IReadOnlyList<FleetVinImportRow> Rows { get; set; } = [];
}
