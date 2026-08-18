using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-856 helpers for vendor-attributed fitment claims.
/// </summary>
public static class VendorFitmentClaimRules
{
    public static bool IsVendorContributed(FitmentClaim claim)
        => ResolveVendorId(claim).HasValue;

    public static int? ResolveVendorId(FitmentClaim claim)
    {
        if (claim.VendorId.HasValue)
            return claim.VendorId;

        return VendorSourceReference.TryParseVendorId(claim.Provenance.SourceReference, out var vendorId)
            ? vendorId
            : null;
    }

    public static bool OwnedByVendor(FitmentClaim claim, int vendorId)
        => ResolveVendorId(claim) == vendorId;
}
