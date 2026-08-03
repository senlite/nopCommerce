using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;

namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import;

public sealed class VehicleAliasImportService
{
    private readonly VehicleAliasApplicationService _vehicleAliasApplicationService;

    public VehicleAliasImportService(VehicleAliasApplicationService vehicleAliasApplicationService)
    {
        _vehicleAliasApplicationService = vehicleAliasApplicationService;
    }

    public async Task<ImportVehicleAliasesResult> ImportAsync(IReadOnlyList<ImportVehicleAliasRow> rows, CancellationToken cancellationToken)
    {
        var result = new ImportVehicleAliasesResult
        {
            TotalRows = rows.Count
        };

        foreach (var row in rows)
        {
            var command = new UpsertVehicleAliasCommand
            {
                NodeType = row.NodeType,
                NodeId = row.NodeId,
                Locale = row.Locale,
                AliasText = row.AliasText
            };

            var commandResult = await _vehicleAliasApplicationService.UpsertAsync(command, cancellationToken);
            if (commandResult.Success)
                result.ImportedRows++;
            else
                result.FailedRows++;
        }

        return result;
    }
}
