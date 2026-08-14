using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Configuration;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Processes durable ERP jobs. Queue claiming is atomic and retries are delayed/bounded, so this
/// task is safe when multiple application nodes or overlapping scheduler invocations execute it.
/// </summary>
public sealed class ErpSyncQueueTask : IScheduleTask
{
    private readonly ErpSyncService _syncService;
    private readonly IOptions<CheckEngineSettings> _settings;

    public ErpSyncQueueTask(ErpSyncService syncService, IOptions<CheckEngineSettings> settings)
    {
        _syncService = syncService;
        _settings = settings;
    }

    public async Task ExecuteAsync()
    {
        if (!_settings.Value.Erp.Enabled)
            return;

        await _syncService.ProcessPendingAsync(default);
    }
}
