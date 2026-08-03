using System;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public sealed class FitmentClaimProvenance
{
    public FitmentSourceKind SourceKind { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? LastVerifiedUtc { get; set; }
}
