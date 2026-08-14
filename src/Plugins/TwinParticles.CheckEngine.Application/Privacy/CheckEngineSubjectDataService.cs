using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Privacy;

/// <summary>
/// Cross-domain data-subject export for Check Engine (FR-960/FR-961). Aggregates all Check
/// Engine-owned personal data for a customer and documents the legal retention holds that apply to
/// data the plugin intentionally does not erase (order-linked financial records).
/// </summary>
public sealed class CheckEngineSubjectDataService
{
    private readonly GaragePrivacyService _garagePrivacyService;
    private readonly ICheckEngineAuditService _auditService;

    public CheckEngineSubjectDataService(
        GaragePrivacyService garagePrivacyService,
        ICheckEngineAuditService auditService)
    {
        _garagePrivacyService = garagePrivacyService;
        _auditService = auditService;
    }

    public async Task<CheckEngineSubjectExport> ExportAsync(
        int customerId,
        string actor,
        CancellationToken cancellationToken)
    {
        var garage = await _garagePrivacyService.ExportAsync(customerId, actor, cancellationToken);

        var export = new CheckEngineSubjectExport
        {
            CustomerId = customerId,
            ExportedUtc = DateTime.UtcNow,
            Garage = garage,
            RetentionHolds = BuildRetentionHolds()
        };

        await _auditService.AppendAsync(
            actor,
            "privacy.subject_export",
            "Customer",
            customerId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"garageVehicles\":{garage.Vehicles.Count},\"garageOems\":{garage.Oems.Count},\"retentionHolds\":{export.RetentionHolds.Count}}}",
            cancellationToken);

        return export;
    }

    private static IReadOnlyList<RetentionHold> BuildRetentionHolds() =>
    [
        new RetentionHold
        {
            DataClass = "order-history",
            Reason = "Financial and tax record-keeping obligations (order history is owned by the host store).",
            Disposition = "Retained by nopCommerce under legal hold; not erased by Check Engine (FR-961)."
        },
        new RetentionHold
        {
            DataClass = "erp-financial-sync",
            Reason = "Cross-system accounting integrity for reconciled transactions.",
            Disposition = "Order-linked ERP synchronization records are retained for the statutory period."
        },
        new RetentionHold
        {
            DataClass = "audit-trail",
            Reason = "Tamper-evident administrative audit obligations (FR-971).",
            Disposition = "Actor references are retained in the append-only audit chain under the retention policy."
        }
    ];
}
