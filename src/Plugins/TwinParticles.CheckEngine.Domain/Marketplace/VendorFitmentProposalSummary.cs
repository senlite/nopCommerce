namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorFitmentProposalSummary
{
    public int PendingReview { get; init; }

    public int Published { get; init; }

    public int Rejected { get; init; }
}
