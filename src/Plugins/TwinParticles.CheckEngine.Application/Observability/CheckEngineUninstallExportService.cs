using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Oem.Admin;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Application.Observability;

public sealed class CheckEngineUninstallExportService
{
    private readonly IVehicleAdminRepository _vehicleRepository;
    private readonly IOemAdminRepository _oemRepository;
    private readonly IFitmentClaimReadRepository _fitmentRepository;
    private readonly ICheckEngineAuditService _auditService;

    public CheckEngineUninstallExportService(
        IVehicleAdminRepository vehicleRepository,
        IOemAdminRepository oemRepository,
        IFitmentClaimReadRepository fitmentRepository,
        ICheckEngineAuditService auditService)
    {
        _vehicleRepository = vehicleRepository;
        _oemRepository = oemRepository;
        _fitmentRepository = fitmentRepository;
        _auditService = auditService;
    }

    public async Task<CheckEngineUninstallExport> ExportAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        var export = new CheckEngineUninstallExport
        {
            ExportedUtc = DateTime.UtcNow,
            Makes = await _vehicleRepository.GetMakesAsync(cancellationToken),
            Models = await _vehicleRepository.GetModelsAsync(cancellationToken),
            Generations = await _vehicleRepository.GetGenerationsAsync(cancellationToken),
            Bodies = await _vehicleRepository.GetBodiesAsync(cancellationToken),
            Engines = await _vehicleRepository.GetEnginesAsync(cancellationToken),
            Markets = await _vehicleRepository.GetMarketsAsync(cancellationToken),
            Configurations = await _vehicleRepository.GetConfigurationsAsync(cancellationToken),
            Aliases = await _vehicleRepository.GetAliasesAsync(cancellationToken),
            Manufacturers = await _oemRepository.GetManufacturersAsync(cancellationToken),
            OemNumbers = await _oemRepository.GetOemNumbersAsync(cancellationToken),
            OemRelations = await _oemRepository.GetRelationsAsync(cancellationToken),
            FitmentClaims = await _fitmentRepository.GetAllClaimsAsync(cancellationToken)
        };

        await _auditService.AppendAsync(
            actor,
            "plugin.uninstall_export",
            "CheckEngine",
            "uninstall",
            beforeJson: null,
            afterJson: $"{{\"configurations\":{export.Configurations.Count},\"oemNumbers\":{export.OemNumbers.Count},\"fitmentClaims\":{export.FitmentClaims.Count}}}",
            cancellationToken);
        return export;
    }
}
