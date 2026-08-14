using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Daily cross-system reconciliation (FR-825). Compares the trailing 24 hours of local order,
/// payment and inventory totals against the ERP system and records an audit event when a
/// discrepancy is detected. No-ops when ERP is disabled.
/// </summary>
public sealed class ErpReconciliationTask : IScheduleTask
{
    private readonly ErpSyncService _syncService;
    private readonly ICheckEngineAuditService _auditService;
    private readonly IOptions<CheckEngineSettings> _settings;
    private readonly ILogger<ErpReconciliationTask> _logger;

    public ErpReconciliationTask(
        ErpSyncService syncService,
        ICheckEngineAuditService auditService,
        IOptions<CheckEngineSettings> settings,
        ILogger<ErpReconciliationTask> logger)
    {
        _syncService = syncService;
        _auditService = auditService;
        _settings = settings;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        if (!_settings.Value.Erp.Enabled)
            return;

        var toUtc = DateTime.UtcNow;
        var report = await _syncService.BuildReconciliationReportAsync(toUtc.AddDays(-1), toUtc, default);

        if (!report.HasFinancialDiscrepancy)
            return;

        _logger.LogWarning("ERP reconciliation detected a discrepancy for window {From:o}-{To:o}", report.WindowFromUtc, report.WindowToUtc);

        await _auditService.AppendAsync(
            "system:erp-reconciliation",
            "erp.reconciliation.discrepancy",
            "ErpReconciliation",
            $"{report.WindowFromUtc:o}/{report.WindowToUtc:o}",
            beforeJson: null,
            afterJson: System.Text.Json.JsonSerializer.Serialize(report.Variances),
            default);
    }
}
