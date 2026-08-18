namespace TwinParticles.CheckEngine.Domain.Dealer;

public enum WarrantyClaimStatus
{
    Submitted = 0,
    UnderReview = 10,
    MoreEvidenceRequested = 20,
    Approved = 30,
    Rejected = 40
}
