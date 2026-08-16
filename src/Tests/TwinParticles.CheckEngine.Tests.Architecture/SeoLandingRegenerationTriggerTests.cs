using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Seo;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Seo;
using TwinParticles.CheckEngine.Infrastructure.Fitment;
using TwinParticles.CheckEngine.Infrastructure.Seo;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SeoLandingRegenerationTriggerTests
{
    [Test]
    public async Task Publishing_A_Claim_Should_Fire_Regeneration_For_Product_And_Vehicle()
    {
        var trigger = new RecordingTrigger();
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository(), options: null, seoLandingRegenerationTrigger: trigger);

        var claim = new FitmentClaim
        {
            Id = 500,
            ProductId = 42,
            VehicleConfigurationId = 77,
            Confidence = 0.95m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsPublished = false,
            IsActive = true
        };

        (await service.TryPublishAsync(claim, CancellationToken.None)).Should().BeTrue();

        trigger.Calls.Should().ContainSingle();
        trigger.Calls[0].Should().Be((42, 77));
    }

    [Test]
    public async Task Approving_A_Claim_Should_Resolve_And_Fire_Regeneration()
    {
        var readRepository = new InMemoryFitmentClaimRepository();
        await readRepository.UpsertAsync(new FitmentClaim
        {
            Id = 0,
            ProductId = 11,
            VehicleConfigurationId = 22,
            Confidence = 0.9m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsActive = true
        }, CancellationToken.None);

        var claim = (await readRepository.GetClaimsAsync(11, 22, CancellationToken.None)).Single();
        var trigger = new RecordingTrigger();
        var service = new FitmentReviewService(readRepository, readRepository, new FakeReviewQueueRepository(), new NoOpAuditService(), trigger);

        await service.ApproveAsync(claim.Id, CancellationToken.None);

        trigger.Calls.Should().ContainSingle();
        trigger.Calls[0].Should().Be((11, 22));
    }

    [Test]
    public async Task Rejecting_A_Claim_Should_Resolve_And_Fire_Regeneration()
    {
        var readRepository = new InMemoryFitmentClaimRepository();
        await readRepository.UpsertAsync(new FitmentClaim
        {
            Id = 0,
            ProductId = 33,
            VehicleConfigurationId = 44,
            Confidence = 0.9m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsPublished = true,
            IsActive = true
        }, CancellationToken.None);

        var claim = (await readRepository.GetClaimsAsync(33, 44, CancellationToken.None)).Single();
        var trigger = new RecordingTrigger();
        var service = new FitmentReviewService(readRepository, readRepository, new FakeReviewQueueRepository(), new NoOpAuditService(), trigger);

        await service.RejectAsync(claim.Id, CancellationToken.None);

        trigger.Calls.Should().ContainSingle();
        trigger.Calls[0].Should().Be((33, 44));
    }

    [Test]
    public async Task Trigger_Should_Regenerate_Both_Landing_Types_In_Both_Locales()
    {
        var repository = new InMemorySeoLandingRepository();
        var sitemap = new InMemorySeoSitemapService();
        var landingService = new SeoLandingService(
            repository,
            new DefaultSeoUrlService(),
            new DefaultSeoStructuredDataService(),
            sitemap,
            new DefaultSeoPerformanceBudgetService(),
            new AlwaysIndexablePolicy());

        var trigger = new SeoLandingRegenerationTrigger(landingService);

        await trigger.OnFitmentPublicationChangedAsync(productId: 7, vehicleConfigurationId: 9, CancellationToken.None);

        var pages = await repository.GetAllAsync(CancellationToken.None);
        pages.Should().HaveCount(4);
        pages.Where(p => p.Type == SeoLandingPageType.Vehicle).Select(p => p.Locale)
            .Should().BeEquivalentTo(new[] { "en", "ar" });
        pages.Where(p => p.Type == SeoLandingPageType.PartForVehicle).Select(p => p.Locale)
            .Should().BeEquivalentTo(new[] { "en", "ar" });

        var urls = await sitemap.GetUrlsAsync(CancellationToken.None);
        urls.Should().Contain("/vehicles/config-9");
        urls.Should().Contain("/ar/vehicles/config-9");
        urls.Should().Contain("/parts/product-7/for/config-9");
        urls.Should().Contain("/ar/parts/product-7/for/config-9");
    }

    [Test]
    public async Task Trigger_Should_Only_Regenerate_Vehicle_Landing_When_Product_Is_Absent()
    {
        var repository = new InMemorySeoLandingRepository();
        var landingService = new SeoLandingService(
            repository,
            new DefaultSeoUrlService(),
            new DefaultSeoStructuredDataService(),
            new InMemorySeoSitemapService(),
            new DefaultSeoPerformanceBudgetService(),
            new AlwaysIndexablePolicy());

        var trigger = new SeoLandingRegenerationTrigger(landingService);

        await trigger.OnFitmentPublicationChangedAsync(productId: 0, vehicleConfigurationId: 9, CancellationToken.None);

        var pages = await repository.GetAllAsync(CancellationToken.None);
        pages.Should().OnlyContain(p => p.Type == SeoLandingPageType.Vehicle);
        pages.Should().HaveCount(2);
    }

    private sealed class RecordingTrigger : ISeoLandingRegenerationTrigger
    {
        public List<(int productId, int vehicleConfigurationId)> Calls { get; } = [];

        public Task OnFitmentPublicationChangedAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
        {
            Calls.Add((productId, vehicleConfigurationId));
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysIndexablePolicy : ISeoIndexabilityPolicy
    {
        public Task<bool> IsVehicleLandingIndexableAsync(int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> IsPartForVehicleLandingIndexableAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FakeWriteRepository : IFitmentClaimWriteRepository
    {
        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeReviewQueueRepository : IFitmentReviewQueueRepository
    {
        public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DequeueAsync(int claimId, string reasonCode, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOpAuditService : TwinParticles.CheckEngine.Domain.Security.ICheckEngineAuditService
    {
        public Task AppendAsync(
            string actor,
            string action,
            string entityType,
            string entityId,
            string? beforeJson,
            string? afterJson,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
