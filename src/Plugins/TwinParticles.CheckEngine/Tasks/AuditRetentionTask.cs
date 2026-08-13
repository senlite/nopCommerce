using System;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Applies the default seven-year audit retention policy. Pruning persists the last removed hash as
/// a chain anchor, so integrity verification remains continuous across the retention boundary.
/// </summary>
public sealed class AuditRetentionTask : IScheduleTask
{
    private readonly IAuditIntegrityService _integrityService;

    public AuditRetentionTask(IAuditIntegrityService integrityService)
    {
        _integrityService = integrityService;
    }

    public async Task ExecuteAsync()
    {
        await _integrityService.PruneAsync(DateTime.UtcNow.AddYears(-7));
    }
}
