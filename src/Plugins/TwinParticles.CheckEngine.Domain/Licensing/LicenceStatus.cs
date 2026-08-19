using System;

namespace TwinParticles.CheckEngine.Domain.Licensing;

public sealed class LicenceStatus
{
    public bool IsActive { get; init; }

    public string State { get; init; } = "inactive";

    public DateTimeOffset? LastHeartbeatUtc { get; init; }

    public string? ReasonCode { get; init; }

    /// <summary>
    /// When false, Check Engine admin mutations return 403 but the storefront keeps working.
    /// </summary>
    public bool AllowsAdminWrite { get; init; }

    public LicenceTier Tier { get; init; }

    /// <summary>
    /// Business-tier and above (FR-870). Independent of <see cref="AllowsAdminWrite"/>.
    /// </summary>
    public bool MarketplaceModuleEntitlement { get; init; }

    /// <summary>
    /// Workshop portal SKU (EP-25). Boolean add-on flag in the licence bundle.
    /// </summary>
    public bool WorkshopPortalEntitlement { get; init; }

    /// <summary>
    /// Fleet portal SKU (EP-26). Boolean add-on flag in the licence bundle.
    /// </summary>
    public bool FleetPortalEntitlement { get; init; }

    /// <summary>
    /// Dealer portal SKU (EP-27). Boolean add-on flag in the licence bundle.
    /// </summary>
    public bool DealerPortalEntitlement { get; init; }
}
