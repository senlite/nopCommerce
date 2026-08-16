using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorOnboardingServiceTests
{
    [Test]
    public async Task Apply_Should_Create_Applied_Vendor_And_Redact_Banking()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);

        var result = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Snapshot!.Vendor.Status.Should().Be(VendorStatus.Applied);
        result.Snapshot.Vendor.HasBankingDetails.Should().BeTrue();
        result.Snapshot.Vendor.BankingSecretProtected.Should().BeNull();
        harness.Repository.StoredSecretFor(result.Snapshot.Vendor.Id).Should().StartWith("enc:");
        harness.Repository.StoredSecretFor(result.Snapshot.Vendor.Id).Should().NotContain("IBAN-SECRET");
    }

    [Test]
    public async Task Approve_Should_Fail_Without_Agreement()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        var applied = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);
        var submitted = await harness.Service.SubmitForReviewAsync(applied.Snapshot!.Vendor.Id, "vendor", CancellationToken.None);

        var approved = await harness.Service.ApproveAsync(submitted.Snapshot!.Vendor.Id, "admin", CancellationToken.None);

        approved.Succeeded.Should().BeFalse();
        approved.ReasonCode.Should().Be(VendorErrorCodes.AgreementRequired);
        (await harness.Repository.GetByIdAsync(submitted.Snapshot.Vendor.Id, CancellationToken.None))!
            .Status.Should().Be(VendorStatus.UnderReview);
    }

    [Test]
    public async Task Approve_Should_Activate_After_Current_Agreement()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        var applied = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);
        var vendorId = applied.Snapshot!.Vendor.Id;
        await harness.Service.SubmitForReviewAsync(vendorId, "vendor", CancellationToken.None);
        await harness.Service.AcceptAgreementAsync(vendorId, "vendor@parts.test", CancellationToken.None);

        var approved = await harness.Service.ApproveAsync(vendorId, "admin", CancellationToken.None);

        approved.Succeeded.Should().BeTrue();
        approved.Snapshot!.Vendor.Status.Should().Be(VendorStatus.Active);
        approved.Snapshot.AcceptedAgreementVersion.Should().Be(VendorAgreementVersions.Default);
    }

    [Test]
    public async Task Approve_Should_Fail_When_Agreement_Version_Is_Stale()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        var applied = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);
        var vendorId = applied.Snapshot!.Vendor.Id;
        await harness.Service.SubmitForReviewAsync(vendorId, "vendor", CancellationToken.None);
        await harness.Service.AcceptAgreementAsync(vendorId, "vendor@parts.test", CancellationToken.None);
        harness.Policy.CurrentAgreementVersion = "2026-09-01";

        var approved = await harness.Service.ApproveAsync(vendorId, "admin", CancellationToken.None);

        approved.ReasonCode.Should().Be(VendorErrorCodes.AgreementStale);
    }

    [Test]
    public async Task Lifecycle_Should_Support_Reject_Suspend_Reinstate_And_Close()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        var first = await SeedUnderReviewAsync(harness);
        (await harness.Service.RejectAsync(first, "admin", "incomplete", CancellationToken.None))
            .Snapshot!.Vendor.Status.Should().Be(VendorStatus.Rejected);

        var second = await SeedActiveAsync(harness);
        (await harness.Service.SuspendAsync(second, "admin", "policy", CancellationToken.None))
            .Snapshot!.Vendor.Status.Should().Be(VendorStatus.Suspended);
        (await harness.Service.ReinstateAsync(second, "admin", CancellationToken.None))
            .Snapshot!.Vendor.Status.Should().Be(VendorStatus.Active);
        (await harness.Service.CloseAsync(second, "admin", "offboard", CancellationToken.None))
            .Snapshot!.Vendor.Status.Should().Be(VendorStatus.Closed);
        (await harness.Service.ReinstateAsync(second, "admin", CancellationToken.None))
            .ReasonCode.Should().Be(VendorErrorCodes.IllegalTransition);
    }

    [Test]
    public async Task Apply_Should_Deny_Without_Marketplace_Entitlement()
    {
        var harness = Harness.Create(marketplaceEntitled: false, applicationsOpen: true);

        var result = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);

        result.ReasonCode.Should().Be(VendorErrorCodes.LicenceDenied);
        harness.Repository.Count.Should().Be(0);
    }

    [Test]
    public async Task Apply_Should_Deny_When_Applications_Are_Closed()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: false);

        var result = await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);

        result.ReasonCode.Should().Be(VendorErrorCodes.ApplicationsClosed);
    }

    [Test]
    public async Task Audit_Should_Never_Record_Banking_Secrets()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        await harness.Service.ApplyAsync(ValidApplication(), CancellationToken.None);

        var entries = harness.Audit.Snapshot();
        entries.Should().NotBeEmpty();
        foreach (var entry in entries)
        {
            entry.AfterJson.Should().NotContain("IBAN-SECRET");
            entry.AfterJson.Should().NotContain("Banking");
            entry.BeforeJson.Should().BeNull();
        }
    }

    [Test]
    public async Task Review_Queue_Should_Include_Applied_And_UnderReview_Only()
    {
        var harness = Harness.Create(marketplaceEntitled: true, applicationsOpen: true);
        var applied = await harness.Service.ApplyAsync(ValidApplication("Alpha Parts"), CancellationToken.None);
        var active = await SeedActiveAsync(harness);

        var queue = await harness.Service.GetReviewQueueAsync(CancellationToken.None);

        queue.Select(item => item.Vendor.Id).Should().Contain(applied.Snapshot!.Vendor.Id);
        queue.Select(item => item.Vendor.Id).Should().NotContain(active);
        queue.Select(item => item.Vendor.BankingSecretProtected).Should().OnlyContain(secret => secret is null);
    }

    private static async Task<int> SeedUnderReviewAsync(Harness harness)
    {
        var applied = await harness.Service.ApplyAsync(ValidApplication($"Vendor {Guid.NewGuid():N}"), CancellationToken.None);
        var submitted = await harness.Service.SubmitForReviewAsync(applied.Snapshot!.Vendor.Id, "vendor", CancellationToken.None);
        return submitted.Snapshot!.Vendor.Id;
    }

    private static async Task<int> SeedActiveAsync(Harness harness)
    {
        var vendorId = await SeedUnderReviewAsync(harness);
        await harness.Service.AcceptAgreementAsync(vendorId, "vendor@parts.test", CancellationToken.None);
        var approved = await harness.Service.ApproveAsync(vendorId, "admin", CancellationToken.None);
        approved.Succeeded.Should().BeTrue();
        return vendorId;
    }

    private static VendorApplication ValidApplication(string legalName = "GMaster Cooling LLC")
        => new()
        {
            LegalName = legalName,
            TradingName = "GMaster",
            ContactEmail = "vendor@parts.test",
            TaxIdsJson = "[\"VAT-99\"]",
            CategoriesCsv = "cooling",
            BankingDetails = "IBAN-SECRET-0001"
        };

    private sealed class Harness
    {
        public required VendorOnboardingService Service { get; init; }
        public required InMemoryVendorRepository Repository { get; init; }
        public required InMemoryCheckEngineAuditService Audit { get; init; }
        public required MarketplaceOnboardingOptions Policy { get; init; }

        public static Harness Create(bool marketplaceEntitled, bool applicationsOpen)
        {
            var repository = new InMemoryVendorRepository();
            var audit = new InMemoryCheckEngineAuditService();
            var policy = new MarketplaceOnboardingOptions
            {
                ApplicationsOpen = applicationsOpen,
                CurrentAgreementVersion = VendorAgreementVersions.Default
            };
            var licence = new StubLicenceService(marketplaceEntitled);
            var service = new VendorOnboardingService(
                repository,
                new PrefixSecretProtector(),
                audit,
                new FixedClock(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero)),
                new MarketplaceLicenceGate(licence),
                policy);

            return new Harness
            {
                Service = service,
                Repository = repository,
                Audit = audit,
                Policy = policy
            };
        }
    }

    private sealed class PrefixSecretProtector : IVendorSecretProtector
    {
        public string? Protect(string? plaintext)
            => string.IsNullOrWhiteSpace(plaintext) ? null : "enc:" + plaintext.Trim();
    }

    private sealed class FixedClock : ICheckEngineClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly bool _marketplace;

        public StubLicenceService(bool marketplace) => _marketplace = marketplace;

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new LicenceStatus
            {
                IsActive = true,
                State = "active",
                AllowsAdminWrite = true,
                Tier = _marketplace ? LicenceTier.Business : LicenceTier.SingleStore,
                MarketplaceModuleEntitlement = _marketplace
            });

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }

    private sealed class InMemoryVendorRepository : IVendorRepository
    {
        private readonly ConcurrentDictionary<int, Vendor> _vendors = new();
        private readonly ConcurrentBag<VendorAgreementAcceptance> _agreements = new();
        private int _nextId = 1;
        private int _nextAgreementId = 1;

        public int Count => _vendors.Count;

        public string? StoredSecretFor(int vendorId)
            => _vendors.TryGetValue(vendorId, out var vendor) ? vendor.BankingSecretProtected : null;

        public Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken)
        {
            vendor.Id = Interlocked.Increment(ref _nextId);
            _vendors[vendor.Id] = Clone(vendor, includeSecret: true);
            return Task.FromResult(vendor.Id);
        }

        public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken)
        {
            var existing = _vendors[vendor.Id];
            var updated = Clone(vendor, includeSecret: false);
            updated.BankingSecretProtected = existing.BankingSecretProtected;
            updated.HasBankingDetails = !string.IsNullOrWhiteSpace(existing.BankingSecretProtected);
            _vendors[vendor.Id] = updated;
            return Task.CompletedTask;
        }

        public Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken)
            => Task.FromResult(_vendors.TryGetValue(id, out var vendor) ? Clone(vendor, includeSecret: false) : null);

        public Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Vendor>>(_vendors.Values
                .Where(vendor => statuses.Contains(vendor.Status))
                .Select(vendor => Clone(vendor, includeSecret: false))
                .ToList());

        public Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken)
        {
            acceptance.Id = Interlocked.Increment(ref _nextAgreementId);
            _agreements.Add(acceptance);
            return Task.CompletedTask;
        }

        public Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult(_agreements
                .Where(item => item.VendorId == vendorId)
                .OrderByDescending(item => item.AcceptedUtc)
                .FirstOrDefault());

        public Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken)
            => Task.FromResult(_agreements.Any(item => item.VendorId == vendorId && item.AgreementVersion == agreementVersion));

        private static Vendor Clone(Vendor vendor, bool includeSecret)
        {
            return new Vendor
            {
                Id = vendor.Id,
                LegalName = vendor.LegalName,
                TradingName = vendor.TradingName,
                ContactEmail = vendor.ContactEmail,
                ApplicantCustomerId = vendor.ApplicantCustomerId,
                TaxIdsJson = vendor.TaxIdsJson,
                CategoriesCsv = vendor.CategoriesCsv,
                Status = vendor.Status,
                BankingSecretProtected = includeSecret ? vendor.BankingSecretProtected : null,
                HasBankingDetails = vendor.HasBankingDetails || !string.IsNullOrWhiteSpace(vendor.BankingSecretProtected),
                ReviewNotes = vendor.ReviewNotes,
                CreatedUtc = vendor.CreatedUtc,
                UpdatedUtc = vendor.UpdatedUtc
            };
        }
    }
}
