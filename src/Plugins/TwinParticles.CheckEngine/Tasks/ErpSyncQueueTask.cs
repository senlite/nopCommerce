using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Erp;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Processes durable ERP jobs. Queue claiming is atomic and retries are delayed/bounded, so this
/// task is safe when multiple application nodes or overlapping scheduler invocations execute it.
/// </summary>
public sealed class ErpSyncQueueTask : IScheduleTask
{
    private readonly ErpSyncService _syncService;

    public ErpSyncQueueTask(ErpSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task ExecuteAsync()
    {
        await _syncService.ProcessPendingAsync(default);
    }
}
