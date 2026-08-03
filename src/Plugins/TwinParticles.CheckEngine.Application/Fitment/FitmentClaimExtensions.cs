using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public static class FitmentClaimExtensions
{
    public static bool SourceKindIsAi(this FitmentClaim claim)
    {
        return claim.Provenance.SourceKind == FitmentSourceKind.AiInference;
    }
}
