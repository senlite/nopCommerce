using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Observability;

public sealed class CheckEngineHealthService
{
    private readonly ISearchIndexHealthService _searchIndexHealthService;
    private readonly ILicenceService _licenceService;
    private readonly IErpClientAdapter _erpClientAdapter;
    private readonly ICheckEngineDatabaseHealthProbe? _databaseHealthProbe;

    public CheckEngineHealthService(
        ISearchIndexHealthService searchIndexHealthService,
        ILicenceService licenceService,
        IErpClientAdapter erpClientAdapter,
        ICheckEngineDatabaseHealthProbe? databaseHealthProbe = null)
    {
        _searchIndexHealthService = searchIndexHealthService;
        _licenceService = licenceService;
        _erpClientAdapter = erpClientAdapter;
        _databaseHealthProbe = databaseHealthProbe;
    }

    public async Task<CheckEngineHealthSnapshot> ProbeAsync(CancellationToken cancellationToken)
    {
        var database = await ProbeDatabaseAsync(cancellationToken);
        var searchIndex = await ProbeSearchAsync(cancellationToken);
        var erp = await ProbeErpAsync(cancellationToken);
        var licence = await ProbeLicenceAsync(cancellationToken);

        var healthy = database && searchIndex && erp && licence;
        return new CheckEngineHealthSnapshot
        {
            Status = healthy ? "healthy" : "degraded",
            Database = database ? "ok" : "unavailable",
            SearchIndex = searchIndex ? "ok" : "degraded",
            Erp = erp ? "ok" : "unavailable",
            Licence = licence ? "active" : "inactive",
            Utc = DateTime.UtcNow
        };
    }

    private async Task<bool> ProbeDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_databaseHealthProbe is null)
            return true;

        try
        {
            return await _databaseHealthProbe.CanQueryAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeSearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _searchIndexHealthService.IsHealthyAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeErpAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await _erpClientAdapter.PullInventorySnapshotAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeLicenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _licenceService.GetStatusAsync(cancellationToken);
            return status.IsActive;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class CheckEngineHealthSnapshot
{
    public string Status { get; init; } = "degraded";

    public string Database { get; init; } = "unavailable";

    public string SearchIndex { get; init; } = "degraded";

    public string Erp { get; init; } = "unavailable";

    public string Licence { get; init; } = "inactive";

    public DateTime Utc { get; init; }
}
