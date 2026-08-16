using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class VendorOnboardingResult
{
    public bool Succeeded { get; init; }

    public string? ReasonCode { get; init; }

    public VendorOnboardingSnapshot? Snapshot { get; init; }

    public static VendorOnboardingResult Fail(string reasonCode)
        => new() { Succeeded = false, ReasonCode = reasonCode };

    public static VendorOnboardingResult Ok(VendorOnboardingSnapshot snapshot)
        => new() { Succeeded = true, Snapshot = snapshot };
}
