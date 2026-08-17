using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class VendorOnboardingService
{
    private readonly IVendorRepository _repository;
    private readonly IVendorSecretProtector _secretProtector;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ICheckEngineClock _clock;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly IMarketplaceOnboardingPolicy _policy;

    public VendorOnboardingService(
        IVendorRepository repository,
        IVendorSecretProtector secretProtector,
        ICheckEngineAuditService auditService,
        ICheckEngineClock clock,
        MarketplaceLicenceGate licenceGate,
        IMarketplaceOnboardingPolicy policy)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _auditService = auditService;
        _clock = clock;
        _licenceGate = licenceGate;
        _policy = policy;
    }

    public async Task<VendorOnboardingResult> ApplyAsync(VendorApplication application, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorOnboardingResult.Fail(VendorErrorCodes.LicenceDenied);

        if (!_policy.ApplicationsOpen)
            return VendorOnboardingResult.Fail(VendorErrorCodes.ApplicationsClosed);

        if (!IsValidApplication(application))
            return VendorOnboardingResult.Fail(VendorErrorCodes.InvalidApplication);

        var now = _clock.UtcNow;
        var vendor = new Vendor
        {
            LegalName = application.LegalName.Trim(),
            TradingName = string.IsNullOrWhiteSpace(application.TradingName) ? null : application.TradingName.Trim(),
            ContactEmail = application.ContactEmail.Trim(),
            ApplicantCustomerId = application.ApplicantCustomerId,
            TaxIdsJson = string.IsNullOrWhiteSpace(application.TaxIdsJson) ? "[]" : application.TaxIdsJson.Trim(),
            CategoriesCsv = application.CategoriesCsv.Trim(),
            Status = VendorStatus.Applied,
            BankingSecretProtected = _secretProtector.Protect(application.BankingDetails),
            HasBankingDetails = !string.IsNullOrWhiteSpace(application.BankingDetails),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        vendor.Id = await _repository.InsertAsync(vendor, cancellationToken);
        await AuditAsync("vendor.apply", vendor.Id, $"{{\"status\":\"Applied\"}}", cancellationToken);
        return VendorOnboardingResult.Ok(await SnapshotAsync(vendor.Id, cancellationToken) ?? new VendorOnboardingSnapshot { Vendor = vendor.RedactSecrets() });
    }

    public Task<VendorOnboardingResult> SubmitForReviewAsync(int vendorId, string actor, CancellationToken cancellationToken)
        => TransitionAsync(vendorId, VendorStatus.Applied, VendorStatus.UnderReview, actor, "vendor.submit", cancellationToken);

    public async Task<VendorOnboardingResult> AcceptAgreementAsync(int vendorId, string actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorOnboardingResult.Fail(VendorErrorCodes.LicenceDenied);

        var vendor = await _repository.GetByIdAsync(vendorId, cancellationToken);
        if (vendor is null)
            return VendorOnboardingResult.Fail(VendorErrorCodes.NotFound);

        if (vendor.Status is VendorStatus.Rejected or VendorStatus.Closed)
            return VendorOnboardingResult.Fail(VendorErrorCodes.IllegalTransition);

        var version = _policy.CurrentAgreementVersion;
        if (await _repository.HasAcceptedAgreementAsync(vendorId, version, cancellationToken))
            return VendorOnboardingResult.Ok((await SnapshotAsync(vendorId, cancellationToken))!);

        await _repository.InsertAgreementAsync(new VendorAgreementAcceptance
        {
            VendorId = vendorId,
            AgreementVersion = version,
            AcceptedUtc = _clock.UtcNow,
            AcceptedBy = string.IsNullOrWhiteSpace(actor) ? "vendor" : actor
        }, cancellationToken);

        await AuditAsync("vendor.agreement.accept", vendorId, $"{{\"version\":\"{version}\"}}", cancellationToken);
        return VendorOnboardingResult.Ok((await SnapshotAsync(vendorId, cancellationToken))!);
    }

    public async Task<VendorOnboardingResult> ApproveAsync(int vendorId, string actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorOnboardingResult.Fail(VendorErrorCodes.LicenceDenied);

        var vendor = await _repository.GetByIdAsync(vendorId, cancellationToken);
        if (vendor is null)
            return VendorOnboardingResult.Fail(VendorErrorCodes.NotFound);

        if (vendor.Status != VendorStatus.UnderReview)
            return VendorOnboardingResult.Fail(VendorErrorCodes.IllegalTransition);

        var requiredVersion = _policy.CurrentAgreementVersion;
        if (!await _repository.HasAcceptedAgreementAsync(vendorId, requiredVersion, cancellationToken))
        {
            var latest = await _repository.GetLatestAgreementAsync(vendorId, cancellationToken);
            return VendorOnboardingResult.Fail(latest is null
                ? VendorErrorCodes.AgreementRequired
                : VendorErrorCodes.AgreementStale);
        }

        return await SetStatusAsync(vendor, VendorStatus.Active, actor, "vendor.approve", cancellationToken);
    }

    public Task<VendorOnboardingResult> RejectAsync(int vendorId, string actor, string? notes, CancellationToken cancellationToken)
        => TransitionAsync(vendorId, VendorStatus.UnderReview, VendorStatus.Rejected, actor, "vendor.reject", cancellationToken, notes);

    public Task<VendorOnboardingResult> SuspendAsync(int vendorId, string actor, string? notes, CancellationToken cancellationToken)
        => TransitionAsync(vendorId, VendorStatus.Active, VendorStatus.Suspended, actor, "vendor.suspend", cancellationToken, notes);

    public Task<VendorOnboardingResult> ReinstateAsync(int vendorId, string actor, CancellationToken cancellationToken)
        => TransitionAsync(vendorId, VendorStatus.Suspended, VendorStatus.Active, actor, "vendor.reinstate", cancellationToken);

    public Task<VendorOnboardingResult> CloseAsync(int vendorId, string actor, string? notes, CancellationToken cancellationToken)
        => TransitionAsync(vendorId, VendorStatus.Active, VendorStatus.Closed, actor, "vendor.close", cancellationToken, notes);

    public async Task<IReadOnlyList<VendorOnboardingSnapshot>> GetReviewQueueAsync(CancellationToken cancellationToken)
    {
        var vendors = await _repository.GetByStatusesAsync(
            [VendorStatus.Applied, VendorStatus.UnderReview],
            cancellationToken);

        var snapshots = new List<VendorOnboardingSnapshot>(vendors.Count);
        foreach (var vendor in vendors)
            snapshots.Add(await AttachAgreementAsync(vendor.RedactSecrets(), cancellationToken));

        return snapshots;
    }

    public Task<VendorOnboardingSnapshot?> GetSnapshotAsync(int vendorId, CancellationToken cancellationToken)
        => SnapshotAsync(vendorId, cancellationToken);

    private async Task<VendorOnboardingResult> TransitionAsync(
        int vendorId,
        VendorStatus from,
        VendorStatus to,
        string actor,
        string action,
        CancellationToken cancellationToken,
        string? notes = null)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorOnboardingResult.Fail(VendorErrorCodes.LicenceDenied);

        var vendor = await _repository.GetByIdAsync(vendorId, cancellationToken);
        if (vendor is null)
            return VendorOnboardingResult.Fail(VendorErrorCodes.NotFound);

        if (vendor.Status != from)
            return VendorOnboardingResult.Fail(VendorErrorCodes.IllegalTransition);

        if (!string.IsNullOrWhiteSpace(notes))
            vendor.ReviewNotes = notes.Trim();

        return await SetStatusAsync(vendor, to, actor, action, cancellationToken);
    }

    private async Task<VendorOnboardingResult> SetStatusAsync(
        Vendor vendor,
        VendorStatus to,
        string actor,
        string action,
        CancellationToken cancellationToken)
    {
        var before = vendor.Status;
        vendor.Status = to;
        vendor.UpdatedUtc = _clock.UtcNow;
        await _repository.UpdateAsync(vendor, cancellationToken);
        await AuditAsync(action, vendor.Id, $"{{\"from\":\"{before}\",\"to\":\"{to}\",\"actor\":\"{actor}\"}}", cancellationToken);
        return VendorOnboardingResult.Ok((await SnapshotAsync(vendor.Id, cancellationToken))!);
    }

    private async Task<VendorOnboardingSnapshot?> SnapshotAsync(int vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(vendorId, cancellationToken);
        return vendor is null ? null : await AttachAgreementAsync(vendor.RedactSecrets(), cancellationToken);
    }

    private async Task<VendorOnboardingSnapshot> AttachAgreementAsync(Vendor vendor, CancellationToken cancellationToken)
    {
        var agreement = await _repository.GetLatestAgreementAsync(vendor.Id, cancellationToken);
        return new VendorOnboardingSnapshot
        {
            Vendor = vendor,
            AcceptedAgreementVersion = agreement?.AgreementVersion,
            AgreementAcceptedUtc = agreement?.AcceptedUtc
        };
    }

    private Task AuditAsync(string action, int vendorId, string afterJson, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            "check-engine",
            action,
            "Vendor",
            vendorId.ToString(),
            beforeJson: null,
            afterJson: afterJson,
            cancellationToken);

    private static bool IsValidApplication(VendorApplication application)
    {
        if (application is null)
            return false;
        if (string.IsNullOrWhiteSpace(application.LegalName))
            return false;
        if (string.IsNullOrWhiteSpace(application.ContactEmail) || !application.ContactEmail.Contains('@'))
            return false;
        if (string.IsNullOrWhiteSpace(application.CategoriesCsv))
            return false;
        if (string.IsNullOrWhiteSpace(application.BankingDetails))
            return false;
        if (string.IsNullOrWhiteSpace(application.TaxIdsJson) || application.TaxIdsJson.Trim() is "[]" or "{}")
            return false;

        return true;
    }
}
