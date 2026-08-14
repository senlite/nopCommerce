namespace TwinParticles.CheckEngine.Domain.Fitment;

public enum FitmentStatus
{
    Fits = 1,
    DoesNotFit = 2,
    Unknown = 3,

    /// <summary>Claim lifecycle state; never a runtime evaluation outcome.</summary>
    Rejected = 4,

    /// <summary>
    /// The vehicle context is missing a qualifier the claim depends on, so the customer must
    /// supply more detail. Required by FR-302; must never be reported as a fit.
    /// </summary>
    NeedsDisambiguation = 5
}
