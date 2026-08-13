using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// NeedsDisambiguation is a new evaluation outcome. These tests pin the behaviour of every
/// consumer so it can never be mistaken for a confirmed fit (AC-026.1).
/// </summary>
[TestFixture]
public class FitmentDisambiguationConsumerTests
{
    private static string LocateStorefrontFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    [Test]
    public async Task Missing_Qualifier_Context_Should_Not_Report_A_Fit()
    {
        var claim = new FitmentClaim
        {
            Id = 1,
            Status = FitmentStatus.Fits,
            Confidence = 0.99m,
            IsPublished = true,
            IsActive = true,
            Qualifier = new FitmentClaimQualifier { SteeringSide = "LHD" }
        };

        var service = new FitmentEvaluationService(new SingleClaimRepository(claim), new NoCache());

        var result = await service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = 1,
            VehicleConfigurationId = 1
        }, CancellationToken.None);

        result.Outcome.Should().Be(FitmentStatus.NeedsDisambiguation);
        result.Outcome.Should().NotBe(FitmentStatus.Fits);
        result.ReasonCode.Should().Be("fitment.steering_side_required");
    }

    [Test]
    public async Task A_Negative_Claim_Should_Outrank_A_More_Confident_Positive_Claim()
    {
        var negative = new FitmentClaim
        {
            Id = 1,
            Status = FitmentStatus.DoesNotFit,
            Confidence = 0.30m,
            IsPublished = true,
            IsActive = true
        };
        var positive = new FitmentClaim
        {
            Id = 2,
            Status = FitmentStatus.Fits,
            Confidence = 0.99m,
            IsPublished = true,
            IsActive = true
        };

        var service = new FitmentEvaluationService(new SingleClaimRepository(negative, positive), new NoCache());

        var result = await service.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = 1,
            VehicleConfigurationId = 1
        }, CancellationToken.None);

        result.Outcome.Should().Be(FitmentStatus.DoesNotFit);
        result.ReasonCode.Should().Be("fitment.negative_claim");
    }

    [Test]
    public void Storefront_Should_Render_A_Distinct_State_For_Needs_Disambiguation()
    {
        var script = File.ReadAllText(LocateStorefrontFile("Content", "checkengine-storefront.js"));

        script.Should().Contain("NeedsDisambiguation",
            "the fitment band must handle the outcome explicitly rather than falling through");
        script.Should().Contain("detailLabel",
            "the disambiguation state needs its own customer-facing copy");
    }

    [Test]
    public void Disambiguation_Copy_Should_Be_Localised()
    {
        var plugin = File.ReadAllText(LocateStorefrontFile("CheckEnginePlugin.cs"));
        var view = File.ReadAllText(LocateStorefrontFile(
            "Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml"));

        foreach (var resource in new[]
                 {
                     "Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail",
                     "Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Hint",
                     "Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Cta"
                 })
        {
            plugin.Should().Contain(resource, "customer-facing strings must be localisable (FR-930)");
            view.Should().Contain(resource, "the theme must pass the localised string to the storefront");
        }
    }

    private sealed class SingleClaimRepository : IFitmentClaimReadRepository
    {
        private readonly IReadOnlyList<FitmentClaim> _claims;

        public SingleClaimRepository(params FitmentClaim[] claims) => _claims = claims;

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult(_claims);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class NoCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
