namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// Vendor lifecycle from docs/19-marketplace-module.md (FR-851).
/// Applied → UnderReview → Active | Rejected; Active → Suspended → Active; Active → Closed.
/// </summary>
public enum VendorStatus
{
    Applied = 0,
    UnderReview = 1,
    Active = 2,
    Rejected = 3,
    Suspended = 4,
    Closed = 5
}
