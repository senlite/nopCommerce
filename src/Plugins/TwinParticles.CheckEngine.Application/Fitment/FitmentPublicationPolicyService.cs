using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

/// <summary>
/// Decides whether a fitment claim may become customer-visible.
///
/// Publication is the only path to the storefront (FR-303), so this is the gate that keeps
/// unreviewed AI output and low-confidence safety-critical claims away from customers
/// (INV-006, AC-028.1).
/// </summary>
public sealed class FitmentPublicationPolicyService
{
    private readonly FitmentPublicationOptions _options;
    private readonly IFitmentClaimWriteRepository _writeRepository;

    public FitmentPublicationPolicyService(
        IFitmentClaimWriteRepository writeRepository,
        FitmentPublicationOptions? options = null)
    {
        _writeRepository = writeRepository;
        _options = options ?? FitmentPublicationOptions.Current;
    }

    public async Task<bool> TryPublishAsync(FitmentClaim claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);

        if (claim.Id <= 0 || !claim.IsActive)
            return false;

        if (claim.IsPublished)
            return true;

        // AI inference is a proposal, never an authority; it reaches customers only after a
        // human review republishes it under a different source.
        if (claim.SourceKindIsAi())
            return false;

        if (claim.Confidence < _options.ResolveThreshold(claim.SafetyClass))
            return false;

        claim.IsPublished = true;
        await _writeRepository.UpsertAsync(claim, cancellationToken);
        await _writeRepository.SetPublishedAsync(claim.Id, true, cancellationToken);
        return true;
    }
}
