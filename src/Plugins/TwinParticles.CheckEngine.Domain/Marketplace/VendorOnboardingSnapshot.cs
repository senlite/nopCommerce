using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorOnboardingSnapshot
{
    public Vendor Vendor { get; init; } = new();

    public string? AcceptedAgreementVersion { get; init; }

    public DateTimeOffset? AgreementAcceptedUtc { get; init; }
}
