using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorOnboardingSnapshot
{
    public Vendor Vendor { get; init; } = new();

    public string? AcceptedAgreementVersion { get; init; }

    public DateTimeOffset? AgreementAcceptedUtc { get; init; }

    /// <summary>
    /// Plaintext apply-time token, returned once on apply so guests can call Status and
    /// AcceptAgreement. Later snapshots omit this.
    /// </summary>
    public string? ApplicantAccessToken { get; init; }

    public VendorOnboardingSnapshot WithAccessToken(string? token)
        => new()
        {
            Vendor = Vendor,
            AcceptedAgreementVersion = AcceptedAgreementVersion,
            AgreementAcceptedUtc = AgreementAcceptedUtc,
            ApplicantAccessToken = token
        };
}
