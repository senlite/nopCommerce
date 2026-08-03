using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportVehicleMatchingService
{
    public void Apply(IReadOnlyList<ImportPipelineRowState> rows)
    {
        foreach (var row in rows)
        {
            if (row.Fields.TryGetValue("vehicleConfigurationId", out var value)
                && int.TryParse(value, out var configurationId)
                && configurationId > 0)
            {
                row.VehicleConfigurationId = configurationId;
                row.VehicleMatchConfidence = 1.0m;
            }
            else
            {
                row.VehicleConfigurationId = null;
                row.VehicleMatchConfidence = 0m;
            }
        }
    }
}
