using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorAgreementAcceptance
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public string AgreementVersion { get; set; } = string.Empty;

    public DateTimeOffset AcceptedUtc { get; set; }

    public string AcceptedBy { get; set; } = string.Empty;
}
